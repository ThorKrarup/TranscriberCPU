# LocalNet Transcriber — Implementation Plan

The current repo is a bare-bones console app scaffold. The plan migrates it into a proper 3-project Clean Architecture .NET 10 Avalonia UI (MVVM) solution targeting Windows, macOS, and Linux.

---

## Phase 1: Solution Restructure & Core Foundation
**Goal:** Replace the console scaffold with the correct solution structure and define the contract layer.

- [x] Restructure the `.sln` to host three projects: `LocalNetTranscriber.Core`, `LocalNetTranscriber.Infrastructure`, `LocalNetTranscriber.UI`
- [x] Set up project references (Infrastructure → Core, UI → Core + Infrastructure)
- [x] In `Core`: define all **models** (`TranscriptionResult`, `AudioFileContext`), **interfaces** (`ITranscriptionService`, `IAudioPreprocessor`, `IFilePickerService`, `IFileSaverService`, `ISettingsService`), and **custom exceptions** (`UnsupportedAudioFormatException`, `ModelLoadException`)
- [x] Zero external NuGet dependencies in Core

---

## Phase 2: Infrastructure — Audio Processing & Transcription Engine
**Goal:** Implement the heavy lifting behind the Core contracts.

- [x] Add NuGet packages: `Whisper.net` 1.9.0, `Whisper.net.Runtime` 1.9.0, `FFMpegCore` 5.4.0 (note: `FFMpegCore.Binaries` does not exist on NuGet — system FFmpeg required)
- [x] Implement `IAudioPreprocessor`: convert `.mp3`/`.m4a` → 16kHz mono WAV to a temp folder using `FFMpegArguments` fluent API (`FfmpegAudioPreprocessor`)
- [x] Implement `ITranscriptionService`: load Whisper model, run inference on a background `Task`, fire progress callbacks (`IProgress<T>`), support `CancellationToken` (`WhisperTranscriptionService`)
- Unit-testable in isolation (no UI dependency)

---

## Phase 3: Avalonia Shell & UI Scaffolding
**Goal:** Wire up the native app host, MVVM structure, and build the single-window dashboard.

- [x] Add NuGet packages: `Avalonia`, `Avalonia.Desktop`, `CommunityToolkit.Mvvm`
- [x] Configure DI in `Program.cs` / `App.axaml.cs` using `Microsoft.Extensions.DependencyInjection` — register Infrastructure implementations against Core interfaces
- [x] Implement `IFilePickerService` and `IFileSaverService` using Avalonia's `StorageProvider` API
- [x] Set up the MVVM scaffold: `MainViewModel` with observable properties and `IRelayCommand` commands
- [x] Build the single `MainWindow.axaml` view with three sections, bound to `MainViewModel`:
  - **Configuration:** "Select Model" + "Select Audio File" buttons with path labels
  - **Transcript Area:** scrollable text output
  - **Action Bar:** Transcribe, Cancel, Export buttons + status label + progress bar
- [x] Enforce contextual button states via `CanExecute` on commands and binding to ViewModel state

---

## Phase 4: Automatic Model Management
**Goal:** Replace manual model file-picking with a zero-friction download-and-cache system.

- [x] Add `WhisperModelSize` enum to `Core` (`Tiny`, `Base`, `Small`, `Medium`) — no Whisper.net dependency
- [x] Add `IModelManager` interface to `Core`:
  - `bool IsModelCached(WhisperModelSize size)`
  - `Task<string> EnsureModelAsync(WhisperModelSize size, IProgress<double>? progress, CancellationToken ct)` — returns local model path, downloading only if needed
- [x] Implement `WhisperModelManager` in `Infrastructure`:
  - Maps `WhisperModelSize` → `Whisper.net`'s `GgmlType`
  - Uses `WhisperGgmlDownloader.GetGgmlModelAsync(type, progress, ct)` to fetch from Hugging Face
  - Stores model files under `<AppData>/LocalNetTranscriber/models/<size>.bin`
  - Returns cached path on subsequent calls with no network activity
- [x] Register `WhisperModelManager` in `App.axaml.cs` DI; replace `Func<string, ITranscriptionService>` factory with `IModelManager`
- [x] Update `MainViewModel`:
  - Replace `ModelPath` string + "Select Model" command with a `SelectedModelSize` property (default `Base`) bound to a ComboBox
  - `TranscribeCommand` calls `IModelManager.EnsureModelAsync(...)` first, then proceeds to preprocessing and transcription
  - Extend the state machine: `Idle → Downloading Model → Transcribing → Done / Error`
  - Show download progress in the shared progress bar; status text reflects download vs transcription phase
- [x] Update `MainWindow.axaml`: replace "Select Model" button with a `ComboBox` of model sizes + a cached/download indicator label

---

## Phase 5: State, Preferences & Export
**Goal:** Deliver the remaining "Should Have" features that complete the MVP experience.

- [x] Implement full app state management in `MainViewModel`: `Idle → Downloading Model → Transcribing → Done / Error`
- [x] Persist selected `WhisperModelSize` in `JsonSettingsService` so the chosen model is remembered across launches
- [x] Implement Export: save transcript to `.txt` via Avalonia's native save dialog (already wired via `IFileSaverService`)
- [x] Ensure "Cancel" safely aborts both model download and in-progress transcription via the shared `CancellationToken`

## Phase 6: Error Handling & Cross-Platform Polish
**Goal:** Make the app robust and ready for daily use on Windows, macOS, and Linux.

- Catch and surface all custom exceptions as friendly Avalonia dialog alerts (unsupported format, corrupt model, download failure, cancelled)
- Validate audio file extension on selection before transcription starts
- Test full flow on Linux, Windows, and macOS
- Clean up temp WAV files (written to `Path.GetTempPath()`) after transcription completes or is cancelled

---

## Phase 7 (Future / Could Have)
Deferred per PRD — implement only after Phase 5 is stable:

- **Batch processing:** queue multiple audio files sequentially
- **Timestamps:** option to embed `[HH:MM:SS]` markers in transcript
- **Subtitle export:** `.srt` / `.vtt` format output

---

**Key dependency chain:** Phase 1 must be done before any other phase. Phases 2 and 3 can progress in parallel once Phase 1 is complete. Phase 4 (model management) can be implemented alongside or after Phase 3 — it requires Phase 2. Phase 5 builds on 3 and 4. Phase 6 requires a working end-to-end flow.
