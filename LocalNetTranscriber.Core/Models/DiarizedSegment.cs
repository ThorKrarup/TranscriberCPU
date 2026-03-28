namespace LocalNetTranscriber.Core.Models;

public record DiarizedSegment(
    TimeSpan Start,
    TimeSpan End,
    string SpeakerId,
    string Text
);
