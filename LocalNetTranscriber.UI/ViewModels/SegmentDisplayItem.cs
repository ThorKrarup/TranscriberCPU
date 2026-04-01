using CommunityToolkit.Mvvm.ComponentModel;
using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.UI.ViewModels;

public sealed class SegmentDisplayItem : ObservableObject
{
    private readonly TimeSpan _start;
    private readonly TimeSpan _end;
    private readonly SpeakerNameEntry _entry;

    public SegmentDisplayItem(DiarizedSegment segment, SpeakerNameEntry entry)
    {
        _start = segment.Start;
        _end   = segment.End;
        _entry = entry;
        Text   = segment.Text;

        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SpeakerNameEntry.DisplayName))
                OnPropertyChanged(nameof(Header));
        };
    }

    public string Header => $"{_entry.DisplayName}  {FormatTime(_start)} – {FormatTime(_end)}";
    public string Text { get; }

    private static string FormatTime(TimeSpan ts) =>
        $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
}
