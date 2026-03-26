namespace LocalNetTranscriber.Core.Interfaces;

public interface IFilePickerService
{
    Task<string?> PickAudioFileAsync();
    Task<string?> PickModelFileAsync();
}
