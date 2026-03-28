using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface ISettingsService
{
    WhisperModelSize SelectedModelSize { get; }
    bool DiarizationEnabled { get; }
    int? KnownSpeakerCount { get; }

    void SaveSelectedModelSize(WhisperModelSize size);
    void SaveDiarizationSettings(bool enabled, int? speakerCount);
}
