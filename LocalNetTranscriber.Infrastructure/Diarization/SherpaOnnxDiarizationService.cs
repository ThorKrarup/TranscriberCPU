using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;
using SherpaOnnx;
using SharpCompress.Readers;

namespace LocalNetTranscriber.Infrastructure.Diarization;

public class SherpaOnnxDiarizationService : IDiarizationService
{
    private static readonly HttpClient SharedHttpClient = new();

    private static readonly string DiarizationDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocalNetTranscriber",
        "diarization");

    private const string SegmentationUrl =
        "https://github.com/k2-fsa/sherpa-onnx/releases/download/speaker-segmentation-models/sherpa-onnx-pyannote-segmentation-3-0.tar.bz2";

    private const string EmbeddingUrl =
        "https://github.com/k2-fsa/sherpa-onnx/releases/download/speaker-recongition-models/nemo_en_titanet_small.onnx";

    private const string SegmentationFileName = "pyannote-segmentation.onnx";
    private const string EmbeddingFileName = "nemo_en_titanet_small.onnx";

    // Approximate compressed download sizes for progress reporting
    private const long ApproxSegmentationBytes = 6_000_000L;
    private const long ApproxEmbeddingBytes = 12_000_000L;

    public async Task<IReadOnlyList<DiarizedSegment>> DiarizeAsync(
        string wavPath,
        int? speakerCount,
        IProgress<double> progress,
        CancellationToken ct)
    {
        Directory.CreateDirectory(DiarizationDir);

        // Download/verify models: 0–20% segmentation, 20–40% embedding
        var segPath = await EnsureSegmentationModelAsync(
            new Progress<double>(p => progress.Report(p * 0.2)), ct);

        var embPath = await EnsureEmbeddingModelAsync(
            new Progress<double>(p => progress.Report(0.2 + p * 0.2)), ct);

        ct.ThrowIfCancellationRequested();

        // Read WAV samples into float[]: 40–42%
        var (samples, sampleRate) = await Task.Run(() => ReadWavSamples(wavPath), ct);
        progress.Report(0.42);

        ct.ThrowIfCancellationRequested();

        // Run diarization: 42–100%
        return await Task.Run(() => RunDiarization(
            segPath, embPath, samples, sampleRate, speakerCount,
            new Progress<double>(p => progress.Report(0.42 + p * 0.58)),
            ct), ct);
    }

    private static IReadOnlyList<DiarizedSegment> RunDiarization(
        string segPath,
        string embPath,
        float[] samples,
        int sampleRate,
        int? speakerCount,
        IProgress<double> progress,
        CancellationToken ct)
    {
        var config = new OfflineSpeakerDiarizationConfig
        {
            Segmentation = new OfflineSpeakerSegmentationModelConfig
            {
                Pyannote = new OfflineSpeakerSegmentationPyannoteModelConfig
                {
                    Model = segPath,
                },
                NumThreads = 2,
                Debug = 0,
                Provider = "cpu",
            },
            Embedding = new SpeakerEmbeddingExtractorConfig
            {
                Model = embPath,
                NumThreads = 2,
                Debug = 0,
                Provider = "cpu",
            },
            Clustering = new FastClusteringConfig
            {
                NumClusters = speakerCount ?? -1,
                Threshold = 0.5f,
            },
            MinDurationOn = 0.2f,
            MinDurationOff = 0.5f,
        };

        using var sd = new OfflineSpeakerDiarization(config);

        var cancelled = false;
        var rawSegments = sd.ProcessWithCallback(samples, (numProcessed, numTotal, _) =>
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                return 1; // non-zero signals sherpa-onnx to stop
            }
            progress.Report((double)numProcessed / numTotal);
            return 0;
        }, IntPtr.Zero);

        if (cancelled)
            ct.ThrowIfCancellationRequested();

        // Map speaker index to a letter label (0 → "Speaker A", 1 → "Speaker B", …)
        return rawSegments
            .Select(seg => new DiarizedSegment(
                Start: TimeSpan.FromSeconds(seg.Start),
                End: TimeSpan.FromSeconds(seg.End),
                SpeakerId: $"Speaker {(char)('A' + seg.Speaker)}",
                Text: string.Empty))
            .ToList();
    }

    private static async Task<string> EnsureSegmentationModelAsync(
        IProgress<double> progress, CancellationToken ct)
    {
        var modelPath = Path.Combine(DiarizationDir, SegmentationFileName);
        if (File.Exists(modelPath))
        {
            progress.Report(1.0);
            return modelPath;
        }

        var tarPath = modelPath + ".download.tmp";
        try
        {
            // Download: 85% of this phase
            await DownloadFileAsync(SegmentationUrl, tarPath, ApproxSegmentationBytes,
                new Progress<double>(p => progress.Report(p * 0.85)), ct);

            progress.Report(0.85);

            // Extract model.onnx from the .tar.bz2: remaining 15%
            await Task.Run(() => ExtractSegmentationModel(tarPath, modelPath), ct);

            progress.Report(1.0);
        }
        finally
        {
            try { File.Delete(tarPath); } catch { /* best-effort cleanup */ }
        }

        return modelPath;
    }

    private static void ExtractSegmentationModel(string tarBz2Path, string outputPath)
    {
        using var fileStream = File.OpenRead(tarBz2Path);
        using var reader = ReaderFactory.Open(fileStream);

        while (reader.MoveToNextEntry())
        {
            if (reader.Entry.IsDirectory) continue;
            if (reader.Entry.Key?.EndsWith("model.onnx", StringComparison.OrdinalIgnoreCase) != true) continue;

            var tempPath = outputPath + ".extract.tmp";
            try
            {
                reader.WriteEntryToFile(tempPath);
                File.Move(tempPath, outputPath, overwrite: true);
            }
            catch
            {
                try { File.Delete(tempPath); } catch { /* best-effort */ }
                throw;
            }
            return;
        }

        throw new InvalidOperationException(
            "model.onnx not found inside the sherpa-onnx segmentation model archive.");
    }

    private static async Task<string> EnsureEmbeddingModelAsync(
        IProgress<double> progress, CancellationToken ct)
    {
        var modelPath = Path.Combine(DiarizationDir, EmbeddingFileName);
        if (File.Exists(modelPath))
        {
            progress.Report(1.0);
            return modelPath;
        }

        var tempPath = modelPath + ".download.tmp";
        try
        {
            await DownloadFileAsync(EmbeddingUrl, tempPath, ApproxEmbeddingBytes, progress, ct);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            throw;
        }

        File.Move(tempPath, modelPath, overwrite: true);
        return modelPath;
    }

    private static async Task DownloadFileAsync(
        string url,
        string destPath,
        long approxBytes,
        IProgress<double> progress,
        CancellationToken ct)
    {
        using var response = await SharedHttpClient.GetAsync(
            url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? approxBytes;

        await using var networkStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream = File.Create(destPath);

        var buffer = new byte[81_920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await networkStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            progress.Report(Math.Min(0.99, (double)totalRead / contentLength));
        }

        progress.Report(1.0);
    }

    /// <summary>
    /// Reads a 16-bit PCM WAV file into a mono float[] array normalised to [-1, 1].
    /// Multi-channel files are downmixed to mono by taking the first channel.
    /// </summary>
    private static (float[] samples, int sampleRate) ReadWavSamples(string wavPath)
    {
        using var fs = File.OpenRead(wavPath);
        using var br = new BinaryReader(fs);

        // RIFF header
        br.ReadBytes(4); // "RIFF"
        br.ReadInt32();  // chunk size
        br.ReadBytes(4); // "WAVE"

        int sampleRate = 16_000;
        short numChannels = 1;
        short bitsPerSample = 16;
        byte[]? dataBytes = null;

        // Walk chunks until we have both fmt and data
        while (br.BaseStream.Position <= br.BaseStream.Length - 8)
        {
            var chunkId = new string(br.ReadChars(4));
            var chunkSize = br.ReadInt32();

            if (chunkId == "fmt ")
            {
                br.ReadInt16();          // audio format (1 = PCM)
                numChannels = br.ReadInt16();
                sampleRate = br.ReadInt32();
                br.ReadInt32();          // byte rate
                br.ReadInt16();          // block align
                bitsPerSample = br.ReadInt16();
                var extra = chunkSize - 16;
                if (extra > 0) br.ReadBytes(extra);
            }
            else if (chunkId == "data")
            {
                dataBytes = br.ReadBytes(chunkSize);
                break;
            }
            else
            {
                // Unknown chunk — skip it (pad to even byte boundary)
                br.ReadBytes(chunkSize + (chunkSize & 1));
            }
        }

        if (dataBytes is null)
            throw new InvalidOperationException($"WAV file has no data chunk: {wavPath}");

        var bytesPerSample = bitsPerSample / 8;
        var monoSampleCount = dataBytes.Length / bytesPerSample / numChannels;
        var samples = new float[monoSampleCount];

        for (int i = 0; i < monoSampleCount; i++)
        {
            // Take first channel; skip remaining channels
            var offset = i * numChannels * bytesPerSample;
            short raw = BitConverter.ToInt16(dataBytes, offset);
            samples[i] = raw / 32768.0f;
        }

        return (samples, sampleRate);
    }
}
