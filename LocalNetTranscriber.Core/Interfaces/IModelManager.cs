using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface IModelManager
{
    bool IsModelCached(WhisperModelSize size);

    /// <summary>
    /// Returns the local file path to the model, downloading it first if not cached.
    /// </summary>
    Task<string> EnsureModelAsync(
        WhisperModelSize size,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
