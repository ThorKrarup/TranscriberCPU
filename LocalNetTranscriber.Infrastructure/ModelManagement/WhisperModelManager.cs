using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;
using Whisper.net.Ggml;

namespace LocalNetTranscriber.Infrastructure.ModelManagement;

public class WhisperModelManager : IModelManager
{
    private static readonly HttpClient SharedHttpClient = new();

    private static readonly string ModelsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LocalNetTranscriber",
        "models");

    // Approximate compressed sizes in bytes, used for download progress reporting
    private static readonly Dictionary<WhisperModelSize, long> ApproximateSizes = new()
    {
        [WhisperModelSize.Tiny]    =    75_000_000L,
        [WhisperModelSize.Base]    =   142_000_000L,
        [WhisperModelSize.Small]   =   466_000_000L,
        [WhisperModelSize.Medium]  = 1_500_000_000L,
        [WhisperModelSize.LargeV3] = 3_100_000_000L,
    };

    public bool IsModelCached(WhisperModelSize size) => File.Exists(GetModelPath(size));

    public async Task<string> EnsureModelAsync(
        WhisperModelSize size,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var path = GetModelPath(size);
        if (File.Exists(path))
            return path;

        Directory.CreateDirectory(ModelsDir);

        var tempPath = path + ".tmp";
        try
        {
            var ggmlType = ToGgmlType(size);
            var downloader = new WhisperGgmlDownloader(SharedHttpClient);
            await using var modelStream = await downloader.GetGgmlModelAsync(ggmlType, cancellationToken: ct);
            await using var fileStream = File.Create(tempPath);

            var buffer = new byte[81_920];
            long totalBytesRead = 0;
            var approxTotal = ApproximateSizes[size];
            int bytesRead;

            while ((bytesRead = await modelStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalBytesRead += bytesRead;
                progress?.Report(Math.Min(0.99, (double)totalBytesRead / approxTotal));
            }

            progress?.Report(1.0);
            await fileStream.FlushAsync(ct);
        }
        catch
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            throw;
        }

        File.Move(tempPath, path, overwrite: true);
        return path;
    }

    private static string GetModelPath(WhisperModelSize size) =>
        Path.Combine(ModelsDir, $"{size.ToString().ToLowerInvariant()}.bin");

    private static GgmlType ToGgmlType(WhisperModelSize size) => size switch
    {
        WhisperModelSize.Tiny    => GgmlType.Tiny,
        WhisperModelSize.Base    => GgmlType.Base,
        WhisperModelSize.Small   => GgmlType.Small,
        WhisperModelSize.Medium  => GgmlType.Medium,
        WhisperModelSize.LargeV3 => GgmlType.LargeV3,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };
}
