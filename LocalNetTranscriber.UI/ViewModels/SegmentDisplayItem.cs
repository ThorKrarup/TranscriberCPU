using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.UI.ViewModels;

public sealed class SegmentDisplayItem
{
    public SegmentDisplayItem(DiarizedSegment segment)
    {
        Header = $"{segment.SpeakerId}  {FormatTime(segment.Start)} – {FormatTime(segment.End)}";
        Text = segment.Text;
    }

    public string Header { get; }
    public string Text { get; }

    private static string FormatTime(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
