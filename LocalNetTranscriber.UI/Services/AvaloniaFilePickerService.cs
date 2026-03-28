using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using LocalNetTranscriber.Core.Interfaces;

namespace LocalNetTranscriber.UI.Services;

public class AvaloniaFilePickerService : IFilePickerService
{
    private static readonly FilePickerFileType AudioFileType = new("Audio Files")
    {
        Patterns = ["*.mp3", "*.m4a", "*.wav", "*.ogg", "*.flac", "*.aac", "*.wma", "*.opus"]
    };

    private static readonly FilePickerFileType ModelFileType = new("Whisper Model Files")
    {
        Patterns = ["*.bin", "*.gguf"]
    };

    public async Task<string?> PickAudioFileAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Audio File",
            AllowMultiple = false,
            FileTypeFilter = [AudioFileType]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickModelFileAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Whisper Model",
            AllowMultiple = false,
            FileTypeFilter = [ModelFileType]
        });

        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow is { } window ? TopLevel.GetTopLevel(window) : null;
        return null;
    }
}
