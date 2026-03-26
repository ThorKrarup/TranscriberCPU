using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(
        string wavFilePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}
