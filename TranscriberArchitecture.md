# High-Level Architecture Layout

**Project Name:** LocalNet Transcriber
**Architecture Pattern:** Clean Architecture / N-Tier
**UI Framework:** Avalonia UI (XAML + MVVM)

## 1. Architectural Philosophy
To strictly enforce the priorities of **maintainability, readability, and modularity**, the application utilizes a Clean Architecture approach. 

The application is divided into separate C# projects (assemblies) within the same .NET 10 Solution. This physical separation prevents UI concerns (like Blazor HTML) from tangling with infrastructure concerns (like Whisper model execution), ensuring the codebase remains highly readable and easy to maintain.

## 2. Project Structure

The .NET 10 solution is divided into four projects:

### A. `LocalNetTranscriber.Core` (Class Library)
**Purpose:** The heart of the application. It defines *what* the application does, without knowing *how* it does it.
* **Dependencies:** None. This project must have zero dependencies on MAUI, Blazor, Whisper.net, or any audio libraries. Pure C# only.
* **Contents:**
  * **Models:** Plain C# objects representing data (e.g., `TranscriptionResult`, `AudioFileContext`, `DiarizedSegment`, `TimedTranscriptSegment`). `TranscriptionResult` carries an optional `IReadOnlyList<DiarizedSegment>? Segments` (null when diarization was not run) and an optional `IReadOnlyList<TimedTranscriptSegment>? TimedSegments` (Whisper's per-segment start/end/text, used to populate speaker segment text during diarization merging).
  * **Interfaces:** Contracts that define the required services (e.g., `ITranscriptionService`, `IAudioPreprocessor`, `IFilePickerService`, `IFileSaverService`, `IModelManager`, `ISettingsService`, `IDialogService`, `IDiarizationService`, `ITranscriptExporter`).
  * **Exceptions:** Custom, readable exceptions (e.g., `UnsupportedAudioFormatException`, `ModelLoadException`).
  * **Enums:** `WhisperModelSize` (`Tiny`, `Base`, `Small`, `Medium`) — defined here so Core stays independent of `Whisper.net`. `ExportFormat` (`PlainText`, `Markdown`) — controls how `ITranscriptExporter` renders a `TranscriptionResult` to a string.
* **Maintainability Benefit:** Anyone reading the `Core` project immediately understands the business rules and capabilities of the app without getting bogged down in external library syntax or UI frameworks.

### B. `LocalNetTranscriber.Infrastructure` (Class Library)
**Purpose:** The "How". This project contains the actual implementation of the heavy lifting and external integrations.
* **Dependencies:** References `LocalNetTranscriber.Core`. This is where external NuGet packages live (e.g., `Whisper.net` 1.9.0, `Whisper.net.Runtime` 1.9.0, `FFMpegCore` 5.4.0, `org.k2fsa.sherpa.onnx` 1.12.34, `org.k2fsa.sherpa.onnx.runtime.linux-x64` 1.12.34, `SharpCompress` 0.38.0). Note: `FFMpegCore.Binaries` does not exist on NuGet — a system-installed FFmpeg is required. Add additional sherpa-onnx runtime packages for Windows/macOS as needed.
* **Contents:**
  * **Transcription:** `WhisperTranscriptionService` implements `ITranscriptionService` using the Whisper engine. In addition to the concatenated full text, it captures each Whisper segment's start/end timestamps and text into `TranscriptionResult.TimedSegments`.
  * **Audio Processing:** `FfmpegAudioPreprocessor` implements `IAudioPreprocessor` to convert MP3s/M4As into the 16kHz WAV format required by Whisper.
  * **Model Management:** `WhisperModelManager` implements `IModelManager`. It maps `WhisperModelSize` to `Whisper.net`'s `GgmlType`, uses `WhisperGgmlDownloader` to fetch models from Hugging Face on first use, stores them in `<AppData>/LocalNetTranscriber/models/`, and returns the cached path on subsequent calls.
  * **Diarization:** `SherpaOnnxDiarizationService` implements `IDiarizationService` using sherpa-onnx's CPU-only segmentation + embedding + clustering pipeline. Models (~20–25 MB total) are cached under `<AppData>/LocalNetTranscriber/diarization/` and downloaded on first use from the sherpa-onnx GitHub releases (pyannote segmentation + NeMo TitaNet Small embedding). Progress is reported in three phases: model downloads (0–40%), WAV decode (40–42%), and diarization (42–100%).
  * **Export:** `TranscriptExporter` implements `ITranscriptExporter`. `Render(result, format)` dispatches on `ExportFormat`: Plain Text emits speaker headers (`SpeakerId  MM:SS – MM:SS`) with paragraph text when diarized, otherwise the raw transcript string; Markdown emits a `# Transcript` heading, a duration/language metadata line, then either `## SpeakerId` sections with italic timestamp ranges (diarized), timed-segment paragraphs, or flat text.
* **Maintainability Benefit:** If a new, better offline AI transcription library is released in the future, only this specific project needs to be rewritten. The UI and the Core remain completely untouched.

### C. `LocalNetTranscriber.UI` (Avalonia Application)
**Purpose:** The presentation layer and application host.
* **Dependencies:** References `LocalNetTranscriber.Core` and `LocalNetTranscriber.Infrastructure`.
* **Contents:**
  * **Views (`.axaml`):** Avalonia XAML files defining the visual layout, dropdowns, buttons, and text areas.
  * **ViewModels:** MVVM view models (using the MVVM Community Toolkit) that hold UI state, expose commands, and delegate work to injected services. Views bind to ViewModels; no logic lives in code-behind.
  * **Application Shell (`App.axaml` / `MainWindow.axaml`):** The native window and application entry point, running on Windows, macOS, and Linux.
  * **Native Wrappers:** Implementations of interfaces that require desktop APIs (e.g., `AvaloniaFilePickerService` and `AvaloniaFileSaverService` via Avalonia's `StorageProvider`; `AvaloniaDialogService` for friendly error alerts; `JsonSettingsService` via `System.Text.Json` — persists model size, `DiarizationEnabled`, `KnownSpeakerCount`, and `SelectedExportFormat`).
  * **Dependency Injection Setup (`Program.cs` / `App.axaml.cs`):** Acts as the "glue", registering Infrastructure implementations (`WhisperModelManager`, `SherpaOnnxDiarizationService`, etc.) against Core interfaces using `Microsoft.Extensions.DependencyInjection`.
* **Maintainability Benefit:** ViewModels remain thin and readable. They only hold state and translate commands into service calls, delegating all actual work to the injected Core services.

### D. `TranscriberCPU.Tests` (xUnit Test Project)
**Purpose:** Validates Core and Infrastructure behaviour in isolation, without a UI host.
* **Dependencies:** References `LocalNetTranscriber.Core` and `LocalNetTranscriber.Infrastructure`.
* **Contents:**
  * **Smoke Tests:** `CoreSmokeTests` — fast, unit-level checks covering models, enums, and exception types defined in Core.
* **Maintainability Benefit:** Infrastructure implementations remain testable without spinning up the Avalonia shell.

## 3. Data Flow & Execution Path

The following outlines how the layers interact during a standard transcription task:

1. **User Action (UI):** The user selects a model size from the dropdown (default: Base) and an audio file, then clicks "Transcribe".
2. **Model Ensure (UI -> Infrastructure):** The ViewModel calls `IModelManager.EnsureModelAsync(size, progress, ct)`. If the model is already cached locally, this returns immediately. If not, `WhisperModelManager` downloads it from Hugging Face, reporting download progress back to the UI.
3. **Delegation (UI -> Core):** With a valid model path in hand, the ViewModel calls `ITranscriptionService.TranscribeAsync(wavPath, progress, ct)`. The ViewModel updates its observable state to trigger a "Transcribing…" binding in the View.
4. **Preprocessing (Infrastructure):** The `FfmpegAudioPreprocessor` converts the audio to a 16kHz mono WAV file in the system temp folder.
5. **Processing (Infrastructure):** The Whisper engine processes the formatted audio on a background `Task`, firing progress events back up to the UI.
6. **Result (Core -> UI):** The service returns a clean `TranscriptionResult` object to the ViewModel.
7. **Diarization (Infrastructure, optional):** If `IsDiarizationEnabled` is on, the ViewModel calls `IDiarizationService.DiarizeAsync` on the same WAV file, producing `IReadOnlyList<DiarizedSegment>` with speaker time windows. The ViewModel then merges `TranscriptionResult.TimedSegments` into each speaker segment: Whisper segments whose midpoint falls within a speaker's time window are concatenated as that segment's text. The merged result is stored back into `TranscriptionResult.Segments`. Status reads `"Diarizing…"` during this pass. The "Number of Speakers" control uses `0` to mean auto-detect (sherpa-onnx threshold clustering); values 1–20 are passed directly as `NumClusters`.
8. **Display (UI):** The ViewModel updates its observable properties; Avalonia bindings automatically refresh the View. When `Segments` is present, each turn is rendered as a `SegmentDisplayItem` — a bold header with speaker ID and timestamps, followed by the merged transcript text. The flat `TextBox` is hidden and replaced by a `ScrollViewer`/`ItemsControl`. The temp WAV file is deleted in the `finally` block.
9. **Export (UI -> Infrastructure):** The user selects an `ExportFormat` (`PlainText` / `Markdown`) from a dropdown (persisted via `ISettingsService`). On "Save", the ViewModel calls `ITranscriptExporter.Render(_lastResult, SelectedExportFormat)` to obtain the formatted string, then passes it — along with the appropriate file extension (`.txt` or `.md`) — to `IFileSaverService.SaveTranscriptAsync`, which opens a native Save File dialog.