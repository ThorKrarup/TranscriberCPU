# Product Requirements Document (PRD)

**Project Name:** LocalNet Transcriber (Working Title)
**Platform:** Desktop Windows/macOS/Linux (.NET 10 via Avalonia UI)
**Primary User:** Single User (Personal Tool)
**Status:** In Development

## 1. Product Overview
The goal of this project is to build a local, standalone desktop application capable of transcribing audio files into text. The application will run entirely offline, ensuring complete data privacy and removing reliance on external cloud services or internet connectivity.

## 2. Technical Stack & Core Principles
* **Framework:** .NET 10
* **UI Framework:** Avalonia UI — a cross-platform XAML-based UI framework that runs natively on Windows, macOS, and Linux without a WebView or browser runtime.
* **Core Philosophy:**
  * **Maintainability over Performance:** Code must be self-documenting, well-organized, and strictly adhere to SOLID principles. We will favor readable loops, clear naming conventions, and abstracted services over highly optimized, complex, or unsafe code blocks.
  * **Modularity:** The transcription engine should be abstracted behind an interface so the underlying AI model/library can be swapped out in the future without touching the Avalonia views or application shell.

## 3. Functional Requirements

### 3.1. Must Have (Core MVP)
* **File Selection:** The user can open a native system file dialog (utilizing Avalonia's `StorageProvider` API) to select local audio files (e.g., `.mp3`, `.wav`, `.m4a`).
* **Offline Transcription:** The application processes the audio file locally and generates a text transcript.
* **Automatic Model Management:** The application manages Whisper models automatically — no manual file picking required. On first launch (or when no model is cached), the app downloads the selected model size from Hugging Face via `Whisper.net`'s built-in `WhisperGgmlDownloader` and stores it in the user's app-data folder. All subsequent launches use the cached model with zero network activity. The transcription process itself is always fully offline.
* **Model Size Selection:** The user can choose a model size (Tiny / Base / Small / Medium) from a dropdown in the configuration area. The default is **Base**. Changing the size triggers a one-time download of that model if it is not already cached.
* **UI Feedback:** The user can clearly see the status of the application (e.g., "Idle", "Downloading Model…", "Transcribing…").
* **Text Output:** The transcribed text is displayed in a readable, scrollable text control within the Avalonia view.
* **Export:** The user can save the transcribed text to a local `.txt` file using native save dialogs.

### 3.2. Should Have
* **Progress Indicator:** A visual progress bar and percentage indicator during both model download and transcription.
* **Model Cache Persistence:** The selected model size is persisted in a lightweight JSON settings file so the app remembers the user's choice across launches.
* **Cancellation:** The user can safely click a "Cancel" button to stop a model download or transcription in progress without crashing the application (utilizing `CancellationToken`).
* **Speaker Diarization:** An opt-in pipeline step that partitions the transcript by speaker, labeling each turn (e.g., `[Speaker A]:`, `[Speaker B]:`). Disabled by default to avoid the added processing cost when not needed. When enabled, the status bar must show a distinct `"Diarizing…"` state separate from `"Transcribing…"`, as they are sequential pipeline steps.
* **Known Speaker Count Hint:** An optional numeric input in the configuration area allowing the user to tell the diarizer how many speakers to expect. Leaving it blank triggers auto-detection. The value is persisted in the same JSON settings file as the model size selection.

### 3.3. Could Have (Future Enhancements)
* **Batch Processing:** Queue multiple audio files to be transcribed sequentially.
* **Timestamps:** Option to include timestamps in the generated transcript (e.g., `[00:01:23] Hello world`).
* **Export Formats:** Exporting to `.srt` or `.vtt` for subtitle use.
* **Speaker Naming:** Allow the user to rename auto-labeled speakers (e.g., `Speaker A` → `"Alice"`) after transcription completes, before exporting.
* **Per-Speaker Export:** Export the transcript segments of a single speaker to a separate file.

## 4. Non-Functional Requirements
* **Privacy & Connectivity:** The application must never make network requests during the transcription process. 100% offline capability is mandatory.
* **Readability:** Avalonia views must be kept small and strictly focused on presentation. Complex logic must be pushed down into injected C# services.
* **Error Handling:** The app must gracefully handle errors (e.g., unsupported file formats, corrupted models) and display user-friendly UI alerts.
* **Performance Constraints:** While raw performance is not the priority, the application UI must remain responsive. Heavy transcription workloads must be offloaded to a background `Task`. When speaker diarization is enabled, users should be warned (via a tooltip on the toggle) that it adds a second CPU-intensive pass over the audio and will increase total processing time.

## 5. UI/UX Requirements
* **Single Page Dashboard:** A clean, single-view layout consisting of:
  * **Configuration Section:** A model size dropdown (Tiny / Base / Small / Medium) with a cached-model indicator; a "Select Audio File" button with the selected filename shown alongside; an "Enable Speaker Diarization" toggle checkbox; and an optional "Number of Speakers" numeric input (shown only when the diarization toggle is on, blank = auto-detect).
  * **Transcript Area:** A large, styled text box to display the ongoing or finished transcript. When diarization is enabled, speaker turns must be visually distinct — each turn prefixed with a colored or bold `[Speaker A]:` label, not inlined into a flat paragraph.
  * **Action Bar:** A "Transcribe" button, a "Cancel" button, a progress indicator (shared across model download, transcription, and diarization phases), and an "Export" button.
* **State Management:** Buttons should be visually disabled contextually using Avalonia's binding system (e.g., "Transcribe" is disabled until an audio file is selected; "Export" is disabled until transcription is complete; "Cancel" is only active during a download, transcription, or diarization). The "Number of Speakers" input must be disabled when the diarization toggle is off.

## 6. Likely External Dependencies
* **UI Framework:** `Avalonia UI` with the MVVM Community Toolkit for view models and data binding.
* **Transcription Engine:** `Whisper.net` (binds to `whisper.cpp`), the standard choice for offline C# transcription.
* **Audio Processing:** `FFMpegCore` + system FFmpeg (must be installed separately). NAudio is not suitable as its format conversion pipeline has significant gaps on Linux and macOS. FFMpegCore provides reliable cross-platform `.mp3`/`.m4a` → 16kHz WAV conversion.
* **Model Download:** `Whisper.net`'s built-in `WhisperGgmlDownloader` — downloads GGML model files from Hugging Face on first use and caches them in the user's app-data folder (`%APPDATA%` / `~/.local/share`).
* **Speaker Diarization:** No widely adopted pure-C# diarization NuGet equivalent to `Whisper.net` exists today. The most viable CPU-only in-process approach is `Microsoft.ML.OnnxRuntime` running a pre-exported speaker embedding or diarization ONNX model (e.g., a pyannote pipeline exported to ONNX). The dependency and model format must be finalized before implementation. The implementation must be hidden behind an `IDiarizationService` interface in `LocalNetTranscriber.Core` so the engine can be swapped without touching the UI.

## 7. Core Data Model Impact

Speaker diarization requires a richer output shape than a flat transcript string. The following additions to `LocalNetTranscriber.Core` are implied by the diarization requirements:

* **`DiarizedSegment`** — a model carrying `TimeSpan Start`, `TimeSpan End`, `string SpeakerId`, and `string Text`. Represents one speaker's contiguous turn.
* **`TranscriptionResult`** — must be extended (or replaced) to optionally carry a `IReadOnlyList<DiarizedSegment> Segments` alongside or instead of a raw string, so that the ViewModel can render speaker-labeled output.
* **`IDiarizationService`** — a new Core interface with a method signature along the lines of `DiarizeAsync(string wavPath, int? speakerCount, IProgress<double>, CancellationToken)` returning the segment list. Infrastructure provides the implementation; the UI never calls the engine directly.

## 8. Out of Scope
* Real-time microphone dictation / live transcription.
* Mobile support (iOS/Android). This PRD is strictly for Desktop (Windows/macOS/Linux).
* Multi-user logins or cloud synchronization.
* Aggressive memory management or hyper-optimization using pointers/unsafe code.
* Auto-updating mechanism.
* Analytics, crash reporting, or telemetry.
* Speaker identification (recognizing a specific known person by voice). This PRD covers diarization only — distinguishing speakers from one another within a single recording, not identifying who they are.