using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using LocalNetTranscriber.Core.Interfaces;

namespace LocalNetTranscriber.UI.Services;

public class AvaloniaFileSaverService : IFileSaverService
{
    public async Task<bool> SaveTranscriptAsync(string text)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return false;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Transcript",
            DefaultExtension = "txt",
            SuggestedFileName = "transcript",
            FileTypeChoices =
            [
                new FilePickerFileType("Text File") { Patterns = ["*.txt"] }
            ]
        });

        if (file is null) return false;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text);
        return true;
    }

    private static TopLevel? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow is { } window ? TopLevel.GetTopLevel(window) : null;
        return null;
    }
}
