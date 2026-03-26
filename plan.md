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

- Add NuGet packages: `Avalonia`, `Avalonia.Desktop`, `CommunityToolkit.Mvvm`
- Configure DI in `Program.cs` / `App.axaml.cs` using `Microsoft.Extensions.DependencyInjection` — register Infrastructure implementations against Core interfaces
- Implement `IFilePickerService` and `IFileSaverService` using Avalonia's `StorageProvider` API
- Set up the MVVM scaffold: `MainViewModel` with observable properties and `IRelayCommand` commands
- Build the single `MainWindow.axaml` view with three sections, bound to `MainViewModel`:
  - **Configuration:** "Select Model" + "Select Audio File" buttons with path labels
  - **Transcript Area:** scrollable text output
  - **Action Bar:** Transcribe, Cancel, Export buttons + status label + progress bar
- Enforce contextual button states via `CanExecute` on commands and binding to ViewModel state

---

## Phase 4: State, Preferences & Export
**Goal:** Deliver the "Should Have" features that complete the MVP experience.

- Implement app state management in `MainViewModel`: `Idle → Loading Model → Transcribing → Done / Error`
- Implement `ISettingsService` using `System.Text.Json` to persist last-used model path in a JSON file in the user's app data folder (no repeated file picking on relaunch)
- Implement Export: save transcript to `.txt` via Avalonia's native save dialog (`StorageProvider.SaveFilePickerAsync`)
- Cancellation: "Cancel" button safely aborts in-progress transcription via `CancellationToken`

---

## Phase 5: Error Handling & Cross-Platform Polish
**Goal:** Make the app robust and ready for daily use on both Windows and macOS.

- Catch and surface all custom exceptions as friendly Avalonia dialog alerts (unsupported format, corrupt model, cancelled)
- Validate file extensions on selection before transcription starts
- Test full flow on Linux, Windows, and macOS
- Clean up temp WAV files (written to `Path.GetTempPath()`) after transcription completes or is cancelled

---

## Phase 6 (Future / Could Have)
Deferred per PRD — implement only after Phase 5 is stable:

- **Batch processing:** queue multiple audio files sequentially
- **Timestamps:** option to embed `[HH:MM:SS]` markers in transcript
- **Subtitle export:** `.srt` / `.vtt` format output

---

**Key dependency chain:** Phase 1 must be done before any other phase. Phases 2 and 3 can progress in parallel once Phase 1 is complete. Phase 4 builds on both. Phase 5 requires a working end-to-end flow.
