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

        using (factory)
        {
            await using var processor = factory.CreateBuilder()
                .WithLanguage("auto")
                .WithProgressHandler(p => progress?.Report(p / 100.0))
                .Build();

            await using var fileStream = File.OpenRead(wavFilePath);

            var sb = new StringBuilder();
            TimeSpan lastEnd = TimeSpan.Zero;
            string detectedLanguage = "auto";

            await foreach (var segment in processor.ProcessAsync(fileStream, cancellationToken))
            {
                sb.Append(segment.Text);
                lastEnd = segment.End;

                if (detectedLanguage == "auto" && !string.IsNullOrEmpty(segment.Language))
                    detectedLanguage = segment.Language;
            }

            return new TranscriptionResult(
                Text: sb.ToString().Trim(),
                Duration: lastEnd,
                Language: detectedLanguage);
        }
    }
}