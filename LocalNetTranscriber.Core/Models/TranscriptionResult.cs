namespace LocalNetTranscriber.Core.Models;

public record TranscriptionResult(
    string Text,
    TimeSpan Duration,
    string Language,
    IReadOnlyList<DiarizedSegment>? Segments = null,
    IReadOnlyList<TimedTranscriptSegment>? TimedSegments = null
);
