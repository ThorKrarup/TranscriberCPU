using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using LocalNetTranscriber.Core.Interfaces;

namespace LocalNetTranscriber.UI.Services;

public class AvaloniaDialogService : IDialogService
{
    public async Task ShowErrorAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner is null) return;

        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 80
        };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 380
                    },
                    okButton
                }
            }
        };

        okButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var owner = GetMainWindow();
        if (owner is null) return false;

        var result = false;

        var proceedButton = new Button
        {
            Content = "Proceed",
            MinWidth = 80
        };
        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 80,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 380
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children = { proceedButton, cancelButton }
                    }
                }
            }
        };

        proceedButton.Click += (_, _) => { result = true;  dialog.Close(); };
        cancelButton.Click  += (_, _) => { result = false; dialog.Close(); };

        await dialog.ShowDialog(owner);
        return result;
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }
}
