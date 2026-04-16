using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LocalNetTranscriber.Core.Exceptions;
using LocalNetTranscriber.Core.Interfaces;
using LocalNetTranscriber.Core.Models;
using LocalNetTranscriber.Infrastructure.Transcription;

namespace LocalNetTranscriber.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly HashSet<string> SupportedAudioFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "m4a", "wav", "ogg", "flac", "aac", "wma", "opus"
    };

    private readonly IFilePickerService _filePicker;
    private readonly IFileSaverService _fileSaver;
    private readonly IAudioPreprocessor _preprocessor;
    private readonly IModelManager _modelManager;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogs;
    private readonly IDiarizationService _diarization;
    private readonly ITranscriptExporter _exporter;

    private CancellationTokenSource? _cts;
    private TranscriptionResult? _lastResult;

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
    private string _transcriptText = string.Empty;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProgressIndeterminate;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCached))]
    [NotifyCanExecuteChangedFor(nameof(TranscribeCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    [NotifyCanExecuteChangedFor(nameof(SelectAudioCommand))]
    private bool _isTranscribing;

    [ObservableProperty]
    private bool _isDiarizationEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(KnownSpeakerCountDisplay))]
    private int? _knownSpeakerCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSegments))]
    private IReadOnlyList<SegmentDisplayItem>? _displaySegments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSpeakerEntries))]
    private ObservableCollection<SpeakerNameEntry>? _speakerNameEntries;

    [ObservableProperty]
    private ExportFormat _selectedExportFormat;

    public static IReadOnlyList<ExportFormat> ExportFormats { get; } = Enum.GetValues<ExportFormat>();

    public string AudioFileName =>
        AudioFilePath is null ? "No file selected" : Path.GetFileName(AudioFilePath);

    public bool IsCached => _modelManager.IsModelCached(SelectedModelSize);

    public bool HasSegments => DisplaySegments?.Count > 0;

    public bool HasSpeakerEntries => SpeakerNameEntries?.Count > 0;

    // 0 is displayed as "auto-detect"; any positive value is the known speaker count.
    public decimal? KnownSpeakerCountDisplay
    {
        get => KnownSpeakerCount ?? 0;
        set
        {
            KnownSpeakerCount = value is null || (int)value == 0 ? null : (int)value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(
        IFilePickerService filePicker,
        IFileSaverService fileSaver,
        IAudioPreprocessor preprocessor,
        IModelManager modelManager,
        ISettingsService settings,
        IDialogService dialogs,
        IDiarizationService diarization,
        ITranscriptExporter exporter)
    {
        _filePicker = filePicker;
        _fileSaver = fileSaver;
        _preprocessor = preprocessor;
        _modelManager = modelManager;
        _settings = settings;
        _dialogs = dialogs;
        _diarization = diarization;
        _exporter = exporter;
        _selectedModelSize = _settings.SelectedModelSize;
        _isDiarizationEnabled = _settings.DiarizationEnabled;
        _knownSpeakerCount = _settings.KnownSpeakerCount;
        _selectedExportFormat = _settings.SelectedExportFormat;
    }

    partial void OnSelectedModelSizeChanged(WhisperModelSize value) =>
        _settings.SaveSelectedModelSize(value);

    partial void OnIsDiarizationEnabledChanged(bool value) =>
        _settings.SaveDiarizationSettings(value, KnownSpeakerCount);

    partial void OnKnownSpeakerCountChanged(int? value) =>
        _settings.SaveDiarizationSettings(IsDiarizationEnabled, value);

    partial void OnSelectedExportFormatChanged(ExportFormat value) =>
        _settings.SaveExportFormat(value);

    [RelayCommand(CanExecute = nameof(CanSelect))]
    private async Task SelectAudioAsync()
    {
        var path = await _filePicker.PickAudioFileAsync();
        if (path is null) return;

        var ext = Path.GetExtension(path).TrimStart('.');
        if (!SupportedAudioFormats.Contains(ext))
        {
            await _dialogs.ShowErrorAsync(
                "Unsupported Audio Format",
                $"The file format '.{ext}' is not supported.\n\n" +
                $"Supported formats: {string.Join(", ", SupportedAudioFormats).ToUpperInvariant()}");
            return;
        }

        AudioFilePath = path;
    }

    // Estimated memory required per model (model size × 1.5 for inference overhead)
    private static readonly Dictionary<WhisperModelSize, long> RequiredMemoryBytes = new()
    {
        [WhisperModelSize.Tiny]    =    200_000_000L,
        [WhisperModelSize.Base]    =    300_000_000L,
        [WhisperModelSize.Small]   =    800_000_000L,
        [WhisperModelSize.Medium]  =  2_500_000_000L,
        [WhisperModelSize.LargeV3] =  4_500_000_000L,
    };

    /// <summary>Returns available RAM + free swap in bytes. Returns long.MaxValue if unreadable.</summary>
    private static long GetAvailableMemoryBytes()
    {
        try
        {
            if (!File.Exists("/proc/meminfo")) return long.MaxValue;

            long memAvailable = 0, swapFree = 0;
            foreach (var line in File.ReadLines("/proc/meminfo"))
            {
                if (line.StartsWith("MemAvailable:"))
                    memAvailable = ParseMemInfoKb(line);
                else if (line.StartsWith("SwapFree:"))
                    swapFree = ParseMemInfoKb(line);
            }
            return (memAvailable + swapFree) * 1024L;
        }
        catch { return long.MaxValue; }
    }

    private static long ParseMemInfoKb(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb : 0;
    }

    [RelayCommand(CanExecute = nameof(CanTranscribe))]
    private async Task TranscribeAsync()
    {
        // Pre-flight memory check — runs before IsTranscribing so UI stays ready
        var required = RequiredMemoryBytes[SelectedModelSize];
        var available = GetAvailableMemoryBytes();
        if (available < required)
        {
            var reqGb  = required  / 1_073_741_824.0;
            var availGb = available / 1_073_741_824.0;
            var proceed = await _dialogs.ShowConfirmAsync(
                "Low Memory Warning",
                $"The {SelectedModelSize} model needs ~{reqGb:F1} GB of free memory " +
                $"but only {availGb:F1} GB is available (RAM + swap).\n\n" +
                "The application may crash during transcription. Proceed anyway?");
            if (!proceed) return;
        }

        IsTranscribing = true;
        Progress = 0;
        TranscriptText = string.Empty;
        DisplaySegments = null;
        SpeakerNameEntries = null;
        _lastResult = null;
        ExportCommand.NotifyCanExecuteChanged();

        _cts = new CancellationTokenSource();
        string? tempWavPath = null;
        string? errorTitle = null;
        string? errorMessage = null;

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
            IsProgressIndeterminate = true;
            StatusText = "Converting audio…";
            var context = new AudioFileContext(
                AudioFilePath!,
                Path.GetExtension(AudioFilePath!).TrimStart('.'));

            tempWavPath = await _preprocessor.ConvertToWavAsync(context, _cts.Token);
            IsProgressIndeterminate = false;

            // Phase 3: Transcribe
            StatusText = "Transcribing…";
            var service = new WhisperTranscriptionService(modelPath);
            var progressReporter = new Progress<double>(p =>
            {
                Progress = p;
                StatusText = $"Transcribing… {p:P0}";
            });

            var result = await service.TranscribeAsync(tempWavPath, progressReporter, _cts.Token);

            // Phase 4: Diarize (optional)
            if (IsDiarizationEnabled)
            {
                Progress = 0;
                StatusText = "Diarizing…";
                var diarizationProgress = new Progress<double>(p =>
                {
                    Progress = p;
                    StatusText = $"Diarizing… {p:P0}";
                });
                var segments = await _diarization.DiarizeAsync(
                    tempWavPath, KnownSpeakerCount, diarizationProgress, _cts.Token);

                // Assign transcript text to each speaker segment.
                // Each Whisper segment goes to the diarization window it overlaps most;
                // if it falls entirely in a gap, it is assigned to the nearest window
                // by midpoint distance. This ensures no Whisper text is silently dropped.
                var timed = result.TimedSegments;
                if (timed is { Count: > 0 })
                {
                    var dsList = segments.ToList();
                    var assigned = new Dictionary<int, List<string>>();
                    for (int i = 0; i < dsList.Count; i++) assigned[i] = [];

                    foreach (var ws in timed)
                    {
                        int bestIdx = 0;
                        var bestOverlap = TimeSpan.MinValue;

                        for (int i = 0; i < dsList.Count; i++)
                        {
                            var ds = dsList[i];
                            var overlapStart = ws.Start > ds.Start ? ws.Start : ds.Start;
                            var overlapEnd   = ws.End   < ds.End   ? ws.End   : ds.End;
                            var overlap = overlapEnd - overlapStart;
                            if (overlap > bestOverlap) { bestOverlap = overlap; bestIdx = i; }
                        }

                        // If no actual overlap, fall back to nearest segment by midpoint
                        if (bestOverlap <= TimeSpan.Zero)
                        {
                            var wsMid = ws.Start + (ws.End - ws.Start) / 2;
                            var bestDist = TimeSpan.MaxValue;
                            for (int i = 0; i < dsList.Count; i++)
                            {
                                var dsMid = dsList[i].Start + (dsList[i].End - dsList[i].Start) / 2;
                                var dist = wsMid > dsMid ? wsMid - dsMid : dsMid - wsMid;
                                if (dist < bestDist) { bestDist = dist; bestIdx = i; }
                            }
                        }

                        assigned[bestIdx].Add(ws.Text);
                    }

                    segments = dsList
                        .Select((ds, i) => ds with { Text = string.Join(" ", assigned[i]) })
                        .ToList();
                }

                result = result with { Segments = segments };
            }

            _lastResult = result;
            ExportCommand.NotifyCanExecuteChanged();

            if (result.Segments?.Count > 0)
            {
                var entries = result.Segments
                    .Select(s => s.SpeakerId).Distinct()
                    .Select(id => new SpeakerNameEntry(id)).ToList();
                var entryMap = entries.ToDictionary(e => e.SpeakerId);
                SpeakerNameEntries = new ObservableCollection<SpeakerNameEntry>(entries);
                var items = result.Segments
                    .Select(s => new SegmentDisplayItem(s, entryMap[s.SpeakerId])).ToList();
                DisplaySegments = items;
                TranscriptText = string.Join("\n\n", items.Select(i =>
                    string.IsNullOrEmpty(i.Text) ? i.Header : $"{i.Header}\n{i.Text}"));
            }
            else
            {
                TranscriptText = result.Text;
            }

            Progress = 1;
            StatusText = $"Done  ·  {result.Duration:hh\\:mm\\:ss}  ·  {result.Language}";
        }
        catch (OperationCanceledException)
        {
            IsProgressIndeterminate = false;
            StatusText = "Cancelled";
            Progress = 0;
        }
        catch (ModelLoadException ex)
        {
            IsProgressIndeterminate = false;
            StatusText = "Model error";
            Progress = 0;
            errorTitle = "Model Error";
            errorMessage = $"Failed to load the Whisper model. The file may be corrupt.\n\n{ex.Message}";
        }
        catch (UnsupportedAudioFormatException ex)
        {
            IsProgressIndeterminate = false;
            StatusText = "Unsupported format";
            Progress = 0;
            errorTitle = "Unsupported Audio Format";
            errorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            IsProgressIndeterminate = false;
            StatusText = "Error";
            Progress = 0;
            errorTitle = "Transcription Error";
            errorMessage = ex.Message;
        }
        finally
        {
            if (tempWavPath is not null)
                TryDelete(tempWavPath);

            _cts.Dispose();
            _cts = null;
            IsTranscribing = false;
        }

        if (errorTitle is not null)
            await _dialogs.ShowErrorAsync(errorTitle, errorMessage!);
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportAsync()
    {
        try
        {
            var nameMap = SpeakerNameEntries?
                .Where(e => !string.IsNullOrWhiteSpace(e.CustomName))
                .ToDictionary(e => e.SpeakerId, e => e.DisplayName)
                ?? new Dictionary<string, string>();
            var content = _exporter.Render(_lastResult!, SelectedExportFormat, nameMap);
            var ext = SelectedExportFormat == ExportFormat.Markdown ? "md" : "txt";
            var saved = await _fileSaver.SaveTranscriptAsync(content, ext);
            if (saved) StatusText = "Transcript saved";
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("Export Error", ex.Message);
        }
    }

    private bool CanSelect => !IsTranscribing;

    private bool CanTranscribe =>
        !string.IsNullOrEmpty(AudioFilePath) &&
        !IsTranscribing;

    private bool CanCancel => IsTranscribing;

    private bool CanExport => _lastResult is not null;

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* best-effort */ }
    }
}
