using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalNetTranscriber.Core.Exceptions;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;
using LocalNetTranscriber.Infrastructure.Transcription;

namespace LocalNetTranscriber.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFilePickerService _filePicker;
    private readonly IFileSaverService _fileSaver;
    private readonly IAudioPreprocessor _preprocessor;
    private readonly IModelManager _modelManager;
    private readonly ISettingsService _settings;

    private CancellationTokenSource? _cts;

    public static IReadOnlyList<WhisperModelSize> ModelSizes { get; } =
        Enum.GetValues<WhisperModelSize>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCached))]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    private WhisperModelSize _selectedModelSize;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioFileName))]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    private string? _audioFilePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCommand))]
    private string _transcriptText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCached))]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectAudioCommand))]
    private bool _isTranscribing;

    public string AudioFileName =>
        AudioFilePath is null ? "No file selected" : Path.GetFileName(AudioFilePath);

    public bool IsCached => _modelManager.IsModelCached(SelectedModelSize);

    public MainViewModel(
        IFilePickerService filePicker,
        IFileSaverService fileSaver,
        IAudioPreprocessor preprocessor,
        IModelManager modelManager,
        ISettingsService settings)
    {
        _filePicker = filePicker;
        _fileSaver = fileSaver;
        _preprocessor = preprocessor;
        _modelManager = modelManager;
        _settings = settings;
        _selectedModelSize = _settings.SelectedModelSize;
    }

    partial void OnSelectedModelSizeChanged(WhisperModelSize value) =>
        _settings.SaveSelectedModelSize(value);

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task SelectAudioAsync()
    {
        var path = await _filePicker.PickAudioFileAsync();
        if (path is null) return;
        AudioFilePath = path;
    }

    [RelayCommand(CanExecute = nameof(CanTranscribe))]
    private async Task TranscribeAsync()
    {
        IsTranscribing = true;
        Progress = 0;
        TranscriptText = string.Empty;

        _cts = new CancellationTokenSource();
        string? tempWavPath = null;

        try
        {
            // Phase 1: Ensure model is available (download if needed)
            StatusText = IsCached
                ? "Loading model…"
                : $"Downloading {SelectedModelSize} model…";

            var modelPath = await _modelManager.EnsureModelAsync(
                SelectedModelSize,
                new Progress<double>(p =>
                {
                    Progress = p;
                    StatusText = $"Downloading {SelectedModelSize} model… {p:P0}";
                }),
                _cts.Token);

            // Refresh cached indicator after download
            OnPropertyChanged(nameof(IsCached));

            // Phase 2: Convert audio to WAV
            Progress = 0;
            StatusText = "Converting audio…";
            var context = new AudioFileContext(
                AudioFilePath!,
                Path.GetExtension(AudioFilePath!).TrimStart('.'));

            tempWavPath = await _preprocessor.ConvertToWavAsync(context, _cts.Token);

            // Phase 3: Transcribe
            StatusText = "Transcribing…";
            var service = new WhisperTranscriptionService(modelPath);
            var progressReporter = new Progress<double>(p =>
            {
                Progress = p;
                StatusText = $"Transcribing… {p:P0}";
            });

            var result = await service.TranscribeAsync(tempWavPath, progressReporter, _cts.Token);

            TranscriptText = result.Text;
            Progress = 1;
            StatusText = $"Done  ·  {result.Duration:hh\\:mm\\:ss}  ·  {result.Language}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
            Progress = 0;
        }
        catch (ModelLoadException ex)
        {
            StatusText = $"Model error: {ex.Message}";
            Progress = 0;
        }
        catch (UnsupportedAudioFormatException ex)
        {
            StatusText = $"Audio error: {ex.Message}";
            Progress = 0;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            Progress = 0;
        }
        finally
        {
            if (tempWavPath is not null)
                TryDelete(tempWavPath);

            _cts.Dispose();
            _cts = null;
            IsTranscribing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        var saved = await _fileSaver.SaveTranscriptAsync(TranscriptText);
        if (saved) StatusText = "Transcript saved";
    }

    private bool CanSelect => !IsTranscribing;

    private bool CanTranscribe =>
        !string.IsNullOrEmpty(AudioFilePath) &&
        !IsTranscribing;

    private bool CanCancel => IsTranscribing;

    private bool CanExport => !string.IsNullOrEmpty(TranscriptText);

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}
