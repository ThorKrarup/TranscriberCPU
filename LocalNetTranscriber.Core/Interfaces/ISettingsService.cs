using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface ISettingsService
{
    WhisperModelSize SelectedModelSize { get; }
    void SaveSelectedModelSize(WhisperModelSize size);
}
