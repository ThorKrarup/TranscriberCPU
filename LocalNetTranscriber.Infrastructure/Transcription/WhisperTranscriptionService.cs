using System.Text;
using LocalNetTranscriber.Core.Exceptions;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;
using Whisper.net;

namespace LocalNetTranscriber.Infrastructure.Transcription;

public class WhisperTranscriptionService : ITranscriptionService
{
    private readonly string _modelPath;

    public WhisperTranscriptionService(string modelPath)
    {
        if (!File.Exists(modelPath))
            throw new ModelLoadException(modelPath, $"Model file not found: '{modelPath}'");

        _modelPath = modelPath;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string wavFilePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        WhisperFactory factory;
        try
        {
            factory = WhisperFactory.FromPath(_modelPath);
        }
        catch (Exception ex)
        {
            throw new ModelLoadException(_modelPath, $"Failed to load Whisper model: {ex.Message}", ex);
        }

        var totalDuration = TryReadWavDuration(wavFilePath);

        using (factory)
        {
            var builder = factory.CreateBuilder().WithLanguage("auto");

            // Fall back to Whisper's internal chunk-based progress when duration is unavailable
            if (totalDuration is null)
                builder = builder.WithProgressHandler(p => progress?.Report(p / 100.0));

            await using var processor = builder.Build();
            await using var fileStream = File.OpenRead(wavFilePath);

            var sb = new StringBuilder();
            var timedSegments = new List<TimedTranscriptSegment>();
            TimeSpan lastEnd = TimeSpan.Zero;
            string detectedLanguage = "auto";

            await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
            {
                sb.Append(segment.Text);
                timedSegments.Add(new TimedTranscriptSegment(segment.Start, segment.End, segment.Text.Trim()));
                lastEnd = segment.End;

                if (detectedLanguage == "auto" && !string.IsNullOrEmpty(segment.Language))
                    detectedLanguage = segment.Language;

                if (totalDuration is not null && totalDuration > TimeSpan.Zero)
                    progress?.Report(Math.Min(segment.End / totalDuration.Value, 1.0));
            }

            return new TranscriptionResult(
                Text: sb.ToString().Trim(),
                Duration: lastEnd,
                Language: detectedLanguage,
                TimedSegments: timedSegments);
        }
    }

    // Reads total audio duration from the WAV RIFF header without decoding audio.
    // Returns null if the header cannot be parsed (e.g. non-PCM or truncated file).
    private static TimeSpan? TryReadWavDuration(string wavFilePath)
    {
        try
        {
            using var fs = new FileStream(wavFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);

            // RIFF header: "RIFF" (4) + chunk size (4) + "WAVE" (4)
            if (br.ReadUInt32() != 0x46464952) return null; // "RIFF"
            br.ReadUInt32(); // chunk size
            if (br.ReadUInt32() != 0x45564157) return null; // "WAVE"

            ushort numChannels = 0, bitsPerSample = 0;
            uint sampleRate = 0, dataSize = 0;
            bool foundFmt = false, foundData = false;

            while (fs.Position < fs.Length - 8)
            {
                uint chunkId   = br.ReadUInt32();
                uint chunkSize = br.ReadUInt32();

                if (chunkId == 0x20746D66) // "fmt "
                {
                    br.ReadUInt16(); // audio format (1 = PCM)
                    numChannels   = br.ReadUInt16();
                    sampleRate    = br.ReadUInt32();
                    br.ReadUInt32(); // byte rate
                    br.ReadUInt16(); // block align
                    bitsPerSample = br.ReadUInt16();
                    // skip any extra fmt bytes
                    if (chunkSize > 16) fs.Seek(chunkSize - 16, SeekOrigin.Current);
                    foundFmt = true;
                }
                else if (chunkId == 0x61746164) // "data"
                {
                    dataSize  = chunkSize;
                    foundData = true;
                    break;
                }
                else
                {
                    fs.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            if (!foundFmt || !foundData || numChannels == 0 || bitsPerSample == 0 || sampleRate == 0)
                return null;

            long totalSamples = dataSize / (bitsPerSample / 8) / numChannels;
            return TimeSpan.FromSeconds((double)totalSamples / sampleRate);
        }
        catch
        {
            return null;
        }
    }
}