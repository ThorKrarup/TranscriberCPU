using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface IAudioPreprocessor
{
    /// <summary>
    /// Converts the audio file to a 16kHz mono WAV in the system temp folder.
    /// Returns the path to the temporary WAV file.
    /// </summary>
    Task<string> ConvertToWavAsync(
        AudioFileContext context,
        CancellationToken cancellationToken = default);
}
