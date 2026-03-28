namespace LocalNetTranscriber.Core.Models;

public record TimedTranscriptSegment(TimeSpan Start, TimeSpan End, string Text);
