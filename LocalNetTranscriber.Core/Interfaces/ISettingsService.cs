namespace LocalNetTranscriber.Core.Interfaces;

public interface ISettingsService
{
    string? LastModelPath { get; }
    void SaveModelPath(string path);
}
