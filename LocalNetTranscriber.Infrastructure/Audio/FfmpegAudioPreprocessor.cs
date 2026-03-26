using FFMpegCore;
using FFMpegCore.Enums;
using LocalNetTranscriber.Core.Exceptions;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Infrastructure.Audio;

public class FfmpegAudioPreprocessor : IAudioPreprocessor
{
    private static readonly HashSet<string> SupportedFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "m4a", "wav", "ogg", "flac", "aac", "wma", "opus"
    };

    public async Task<string> ConvertToWavAsync(
        AudioFileContext context,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedFormats.Contains(context.Format))
            throw new UnsupportedAudioFormatException(
                context.FilePath,
                $"Audio format '{context.Format}' is not supported. Supported formats: {string.Join(", ", SupportedFormats)}");

        var outputPath = Path.Combine(Path.GetTempPath(), $"transcriber_{Guid.NewGuid():N}.wav");

        try
        {
            await FFMpegArguments
                .FromFileInput(context.FilePath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithAudioSamplingRate(16000)
                    .WithCustomArgument("-ac 1")
                    .ForceFormat("wav"))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();
        }
        catch (OperationCanceledException)
        {
            TryDelete(outputPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDelete(outputPath);
            throw new UnsupportedAudioFormatException(
                context.FilePath,
                $"Failed to convert '{context.FilePath}' to WAV: {ex.Message}");
        }

        return outputPath;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}