using CommunityToolkit.Mvvm.ComponentModel;

namespace LocalNetTranscriber.UI.ViewModels;

public partial class SpeakerNameEntry : ObservableObject
{
    public string SpeakerId { get; }

    [ObservableProperty]
    private string _customName = string.Empty;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(CustomName) ? SpeakerId : CustomName.Trim();

    public SpeakerNameEntry(string speakerId) => SpeakerId = speakerId;

    partial void OnCustomNameChanged(string value) =>
        OnPropertyChanged(nameof(DisplayName));
}
