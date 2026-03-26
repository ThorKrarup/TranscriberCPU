namespace LocalNetTranscriber.Core.Models;

public record TranscriptionResult(
    string Text,
    TimeSpan Duration,
    string Language
);
