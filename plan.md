# LocalNet Transcriber — Implementation Plan

The current repo is a bare-bones console app scaffold. The plan migrates it into a proper 3-project Clean Architecture .NET 10 Avalonia UI (MVVM) solution targeting Windows, macOS, and Linux.

---

## Phase 1: Solution Restructure & Core Foundation ✓

---

## Phase 2: Infrastructure — Audio Processing & Transcription Engine ✓

---

## Phase 3: Avalonia Shell & UI Scaffolding ✓

---

## Phase 4: Automatic Model Management ✓

---

## Phase 5: State, Preferences & Export ✓

---

## Phase 6: Error Handling & Cross-Platform Polish ✓

---

## Phase 7: Core Diarization Contracts & Data Model ✓

Lay the clean architecture foundation for diarization. No external libraries touched — pure C# only.

- **`DiarizedSegment` model** (`Core/Models/`) — `TimeSpan Start`, `TimeSpan End`, `string SpeakerId`, `string Text`
- **`TranscriptionResult` update** — add `IReadOnlyList<DiarizedSegment>? Segments` (nullable; null when diarization was not run)
- **`IDiarizationService` interface** (`Core/Interfaces/`) — `Task<IReadOnlyList<DiarizedSegment>> DiarizeAsync(string wavPath, int? speakerCount, IProgress<double> progress, CancellationToken ct)`
- **Settings model update** — add `bool DiarizationEnabled` and `int? KnownSpeakerCount` to the existing settings record; update `JsonSettingsService` to persist them

---

## Phase 8: Infrastructure — Diarization Engine ✓

Implement `IDiarizationService` using **sherpa-onnx**, which provides a complete CPU-only segmentation + embedding + clustering pipeline with a first-party C# NuGet and an official dotnet example.

**Resolved decisions:**
- **NuGet:** `org.k2fsa.sherpa.onnx` 1.12.34 + `org.k2fsa.sherpa.onnx.runtime.linux-x64` 1.12.34 (targets `netstandard2.0`, compatible with net10). Add platform runtime packages for Windows/macOS as needed.
- **Segmentation model:** `sherpa-onnx-pyannote-segmentation-3-0/model.onnx` (5.7 MB) — downloaded from the sherpa-onnx `speaker-segmentation-models` GitHub release. Pulling from the sherpa-onnx releases page directly (not HuggingFace) avoids the HuggingFace account gate entirely. The underlying license is MIT.
- **Embedding model:** `nemo_en_titanet_small.onnx` (~10–15 MB) — downloaded from the sherpa-onnx `speaker-recongition-models` GitHub release. Trained on English speech; best fit for English-language use.
- **Total model footprint:** ~20–25 MB on disk.
- **CPU-only:** confirmed — the standard NuGet runtime packages ship CPU-only native binaries.
- **Reference example:** `dotnet-examples/offline-speaker-diarization/Program.cs` in the k2-fsa/sherpa-onnx repo.

**Implementation tasks:**
- Add `org.k2fsa.sherpa.onnx` and the appropriate runtime NuGet(s) to `LocalNetTranscriber.Infrastructure`
- Implement `SherpaOnnxDiarizationService : IDiarizationService` — wraps `OfflineSpeakerDiarization`, loads models from cached paths, calls `ProcessWithCallback` to stream progress, and maps the output segments to `DiarizedSegment`
- Cache both model files under `<AppData>/LocalNetTranscriber/diarization/` — same pattern as `WhisperModelManager`. Download on first use if not present (models are on GitHub releases, not HuggingFace, so no `WhisperGgmlDownloader` equivalent; use `HttpClient` with the same progress-reporting pattern)
- Expose `int? NumClusters` (maps to `config.Clustering.NumClusters`) and a threshold fallback for auto-detect (`config.Clustering.Threshold`) so the ViewModel can pass through the user's speaker count hint
- Register `SherpaOnnxDiarizationService` against `IDiarizationService` in `App.axaml.cs` DI setup

---

## Phase 9: UI — Diarization Controls, State & Rendering ✓

Surface diarization in the ViewModel and View without leaking engine details into the UI layer.

- **ViewModel additions:**
  - `bool IsDiarizationEnabled` (observable, two-way bound, persisted via `ISettingsService`)
  - `int? KnownSpeakerCount` (observable, two-way bound, persisted)
  - Extend the transcription command to run `IDiarizationService.DiarizeAsync` after `ITranscriptionService.TranscribeAsync` when enabled, producing a merged `TranscriptionResult`
  - Add `"Diarizing…"` as a distinct application status state alongside `"Transcribing…"`
- **View additions (`MainView.axaml`):**
  - "Enable Speaker Diarization" `CheckBox` in the configuration section
  - "Number of Speakers" `NumericUpDown` input — visible and enabled only when the checkbox is checked
  - Tooltip on the checkbox: "Adds a second CPU-intensive pass. Increases total processing time."
- **Transcript rendering:** When `Segments` is populated on `TranscriptionResult`, render each turn as a distinct paragraph prefixed with a bold `[Speaker A]:` label rather than flat text. Whisper's timed segments (`TimedTranscriptSegment`) are matched to speaker windows by midpoint and concatenated as each segment's text.
- **State management:** "Number of Speakers" input disabled when diarization toggle is off; "Cancel" button covers the diarization pass via the shared `CancellationToken`
- **Speaker count control:** `NumericUpDown` uses `0` to represent auto-detect (maps to `null` / sherpa-onnx threshold clustering); values 1–20 are passed directly as `NumClusters`. Tooltip documents this behaviour.

---

## Phase 10: Export Format Selection ✓

Surface `ITranscriptExporter` in the UI so the user can choose between Plain Text and Markdown before saving.

- **`ExportFormat` enum** (`Core/Models/`) — `PlainText`, `Markdown`
- **`ITranscriptExporter` interface** (`Core/Interfaces/`) — `string Render(TranscriptionResult result, ExportFormat format)`
- **`TranscriptExporter` implementation** (`Infrastructure/Export/`) — Plain Text renders speaker headers + timestamps when diarized, otherwise raw text; Markdown renders a `# Transcript` heading with duration/language metadata, then either diarized sections (`## SpeakerId` with italic timestamp range) or timed-segment paragraphs or flat text.
- **`ISettingsService` update** — adds `ExportFormat SelectedExportFormat { get; }` and `void SaveExportFormat(ExportFormat format)`; `IFileSaverService.SaveTranscriptAsync` gains a `suggestedExtension` parameter so `.md` vs `.txt` is passed at call time.
- **ViewModel additions** — `ExportFormat SelectedExportFormat` observable, two-way bound and persisted; `ExportFormats` static list for the dropdown. Save command calls `_exporter.Render(_lastResult, SelectedExportFormat)` then `_fileSaver.SaveTranscriptAsync(content, extension, "transcript")`.
- **View additions** — Export format `ComboBox` alongside the Save button; bound to `SelectedExportFormat`.

---

## Phase 11: Speaker Naming ✓

Allow users to replace auto-generated speaker IDs ("Speaker A", "Speaker B") with custom labels ("Alice", "Bob") after diarization completes, before export. The `TranscriptionResult` core model stays immutable; the name mapping is a ViewModel-only, per-session concern applied at render time.

### New file

**`UI/ViewModels/SpeakerNameEntry.cs`** — `ObservableObject` with:
- `string SpeakerId` — read-only original ID
- `[ObservableProperty] string _customName` — two-way bound to a TextBox
- `string DisplayName` — computed: `CustomName.Trim()` if non-empty, else `SpeakerId`
- `partial void OnCustomNameChanged` → fires `OnPropertyChanged(nameof(DisplayName))`

### Modified files

**`UI/ViewModels/SegmentDisplayItem.cs`** — convert to `ObservableObject`; constructor takes `(DiarizedSegment, SpeakerNameEntry)`; store raw time spans + entry ref; subscribe to `entry.PropertyChanged` and raise `OnPropertyChanged(nameof(Header))` when `DisplayName` changes; `Header` becomes a computed property `$"{_entry.DisplayName}  {FormatTime(_start)} – {FormatTime(_end)}"`.

**`UI/ViewModels/MainViewModel.cs`**:
- Add `ObservableCollection<SpeakerNameEntry>? SpeakerNameEntries` + `bool HasSpeakerEntries`
- Reset `SpeakerNameEntries = null` at the start of each transcription
- After diarization: build one `SpeakerNameEntry` per unique speaker (in order of first appearance), create an `entryMap`, then construct `DisplaySegments` as `SegmentDisplayItem(seg, entryMap[seg.SpeakerId])`
- In `ExportAsync`: build `nameMap` from non-blank entries → call `_exporter.Render(_lastResult!, SelectedExportFormat, nameMap)`

**`Core/Interfaces/ITranscriptExporter.cs`** — add overload:
```csharp
string Render(TranscriptionResult result, ExportFormat format,
              IReadOnlyDictionary<string, string> speakerNames);
```

**`Infrastructure/Export/TranscriptExporter.cs`** — existing `Render(result, format)` delegates to the new overload with an empty dict; `RenderPlainText` and `RenderMarkdown` resolve `seg.SpeakerId` via `speakerNames.TryGetValue(id, out var n) ? n : id`.

**`UI/Views/MainWindow.axaml`** — insert a "Speaker Labels" panel (`DockPanel.Dock="Top"`, `IsVisible="{Binding HasSpeakerEntries}"`) after the separator and before the transcript grid; `ItemsControl` over `SpeakerNameEntries` with a two-column row template: read-only `SpeakerId` label + `TextBox` bound to `CustomName` with placeholder text.

### Data flow

```
User types → CustomName (TwoWay) → OnCustomNameChanged → DisplayName changes
  → SegmentDisplayItem raises PropertyChanged("Header") → preview updates live

Export clicked → nameMap built from non-blank entries
  → _exporter.Render(result, format, nameMap) → IDs resolved; fallback to raw ID
```

### Edge cases

- No diarization: `HasSpeakerEntries` false, panel hidden, empty map, exporter uses raw IDs — no change from Phase 10 behaviour.
- New transcription: reset block clears `SpeakerNameEntries` before the new result is populated.
- Partial naming: blank entries are excluded from the export map; the exporter fallback covers them.

---

## Phase 12: Progress Bar Consistency

**Problem 1 — Transcription progress jumps in large uneven increments**

*Root cause:* `WhisperTranscriptionService` uses Whisper.net's `WithProgressHandler`, which fires based on Whisper's internal 30-second chunk boundaries — not actual audio time. For a 3-minute recording that means only 6 updates (0 % → 17 % → 33 % → 50 % → 67 % → 83 % → 100 %), with long stalls between them.

*Fix:* Read total audio duration from the WAV file's RIFF header before processing begins. In the `ProcessAsync` loop, report `segment.End / totalDuration` after each segment lands. Falls back to `WithProgressHandler` only when the header cannot be parsed.

**Problem 2 — Progress bar shows no activity during audio conversion**

*Root cause:* FFmpeg conversion (`IAudioPreprocessor.ConvertToWavAsync`) has no progress callback, so `Progress` sits at 0 and the bar appears frozen during the entire conversion step.

*Fix:* Add `IsProgressIndeterminate` observable property to `MainViewModel`; set it to `true` before conversion and `false` after. Bind `IsIndeterminate` on the `ProgressBar` in `MainWindow.axaml` so the bar shows an animated stripe during conversion.

**Files changed:**
- `LocalNetTranscriber.Infrastructure/Transcription/WhisperTranscriptionService.cs` — add `TryReadWavDuration`, replace `WithProgressHandler` with per-segment reporting
- `LocalNetTranscriber.UI/ViewModels/MainViewModel.cs` — add `IsProgressIndeterminate`, set around audio conversion
- `LocalNetTranscriber.UI/Views/MainWindow.axaml` — bind `IsIndeterminate` on `ProgressBar`

---

## Future / Could Have

Deferred — implement only after Phase 11 is stable:

- **Per-speaker export:** Save one `.txt` file per speaker from a diarized result
- **Batch processing:** Queue multiple audio files to be transcribed sequentially
- **Timestamps:** Option to embed `[HH:MM:SS]` markers in the transcript
- **Subtitle export:** `.srt` / `.vtt` format output

---

**Key dependency chain:** Phases 1–11 complete. Phase 12 in progress. Future items are deferred.
