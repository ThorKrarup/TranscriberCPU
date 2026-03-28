using LocalNetTranscriber.Core.Models;

namespace LocalNetTranscriber.Core.Interfaces;

public interface IDiarizationService
{
    Task<IReadOnlyList<DiarizedSegment>> DiarizeAsync(
        string wavPath,
        int? speakerCount,
        IProgress<double> progress,
        CancellationToken ct);
}
