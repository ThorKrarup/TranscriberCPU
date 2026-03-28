# High-Level Architecture Layout

**Project Name:** LocalNet Transcriber
**Architecture Pattern:** Clean Architecture / N-Tier
**UI Framework:** Avalonia UI (XAML + MVVM)

## 1. Architectural Philosophy
To strictly enforce the priorities of **maintainability, readability, and modularity**, the application utilizes a Clean Architecture approach. 

The application is divided into separate C# projects (assemblies) within the same .NET 10 Solution. This physical separation prevents UI concerns (like Blazor HTML) from tangling with infrastructure concerns (like Whisper model execution), ensuring the codebase remains highly readable and easy to maintain.

## 2. Project Structure

The .NET 10 solution is divided into three primary projects:

### A. `LocalNetTranscriber.Core` (Class Library)
**Purpose:** The heart of the application. It defines *what* the application does, without knowing *how* it does it.
* **Dependencies:** None. This project must have zero dependencies on MAUI, Blazor, Whisper.net, or any audio libraries. Pure C# only.
* **Contents:**
  * **Models:** Plain C# objects representing data (e.g., `TranscriptionResult`, `AudioFileContext`).
  * **Interfaces:** Contracts that define the required services (e.g., `ITranscriptionService`, `IAudioPreprocessor`, `IFilePickerService`, `IModelManager`).
  * **Exceptions:** Custom, readable exceptions (e.g., `UnsupportedAudioFormatException`, `ModelLoadException`).
  * **Enums:** `WhisperModelSize` (`Tiny`, `Base`, `Small`, `Medium`) — defined here so Core stays independent of `Whisper.net`.
* **Maintainability Benefit:** Anyone reading the `Core` project immediately understands the business rules and capabilities of the app without getting bogged down in external library syntax or UI frameworks.

### B. `LocalNetTranscriber.Infrastructure` (Class Library)
**Purpose:** The "How". This project contains the actual implementation of the heavy lifting and external integrations.
* **Dependencies:** References `LocalNetTranscriber.Core`. This is where external NuGet packages live (e.g., `Whisper.net`, `FFMpegCore`, `FFMpegCore.Binaries`).
* **Contents:**
  * **Transcription:** `WhisperTranscriptionService` implements `ITranscriptionService` using the Whisper engine.
  * **Audio Processing:** `FfmpegAudioPreprocessor` implements `IAudioPreprocessor` to convert MP3s/M4As into the 16kHz WAV format required by Whisper.
  * **Model Management:** `WhisperModelManager` implements `IModelManager`. It maps `WhisperModelSize` to `Whisper.net`'s `GgmlType`, uses `WhisperGgmlDownloader` to fetch models from Hugging Face on first use, stores them in `<AppData>/LocalNetTranscriber/models/`, and returns the cached path on subsequent calls.
* **Maintainability Benefit:** If a new, better offline AI transcription library is released in the future, only this specific project needs to be rewritten. The UI and the Core remain completely untouched.

### C. `LocalNetTranscriber.UI` (Avalonia Application)
**Purpose:** The presentation layer and application host.
* **Dependencies:** References `LocalNetTranscriber.Core` and `LocalNetTranscriber.Infrastructure`.
* **Contents:**
  * **Views (`.axaml`):** Avalonia XAML files defining the visual layout, dropdowns, buttons, and text areas.
  * **ViewModels:** MVVM view models (using the MVVM Community Toolkit) that hold UI state, expose commands, and delegate work to injected services. Views bind to ViewModels; no logic lives in code-behind.
  * **Application Shell (`App.axaml` / `MainWindow.axaml`):** The native window and application entry point, running on Windows, macOS, and Linux.
  * **Native Wrappers:** Implementations of interfaces that require desktop APIs (e.g., `AvaloniaFilePickerService` via Avalonia's `StorageProvider`; `JsonSettingsService` via `System.Text.Json`).
  * **Dependency Injection Setup (`Program.cs` / `App.axaml.cs`):** Acts as the "glue", registering Infrastructure implementations (including `WhisperModelManager`) against Core interfaces using `Microsoft.Extensions.DependencyInjection`.
* **Maintainability Benefit:** ViewModels remain thin and readable. They only hold state and translate commands into service calls, delegating all actual work to the injected Core services.

## 3. Data Flow & Execution Path

The following outlines how the layers interact during a standard transcription task:

1. **User Action (UI):** The user selects a model size from the dropdown (default: Base) and an audio file, then clicks "Transcribe".
2. **Model Ensure (UI -> Infrastructure):** The ViewModel calls `IModelManager.EnsureModelAsync(size, progress, ct)`. If the model is already cached locally, this returns immediately. If not, `WhisperModelManager` downloads it from Hugging Face, reporting download progress back to the UI.
3. **Delegation (UI -> Core):** With a valid model path in hand, the ViewModel calls `ITranscriptionService.TranscribeAsync(wavPath, progress, ct)`. The ViewModel updates its observable state to trigger a "Transcribing…" binding in the View.
4. **Preprocessing (Infrastructure):** The `FfmpegAudioPreprocessor` converts the audio to a 16kHz mono WAV file in the system temp folder.
5. **Processing (Infrastructure):** The Whisper engine processes the formatted audio on a background `Task`, firing progress events back up to the UI.
6. **Result (Core -> UI):** The service returns a clean `TranscriptionResult` object to the ViewModel.
7. **Display (UI):** The ViewModel updates its observable properties; Avalonia bindings automatically refresh the View's text area and re-enable the interface buttons. The temp WAV file is deleted in the `finally` block.