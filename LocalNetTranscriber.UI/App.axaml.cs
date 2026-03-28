using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Infrastructure.Audio;
using LocalNetTranscriber.Infrastructure.Diarization;
using LocalNetTranscriber.Infrastructure.ModelManagement;
using LocalNetTranscriber.UI.Services;
using LocalNetTranscriber.UI.ViewModels;
using LocalNetTranscriber.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LocalNetTranscriber.UI;

public partial class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IAudioPreprocessor, FfmpegAudioPreprocessor>();
        services.AddSingleton<IModelManager, WhisperModelManager>();
        services.AddSingleton<IFilePickerService, AvaloniaFilePickerService>();
        services.AddSingleton<IFileSaverService, AvaloniaFileSaverService>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IDiarizationService, SherpaOnnxDiarizationService>();
        services.AddTransient<MainViewModel>();
    }
}
