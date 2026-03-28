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

### 3.3. Could Have (Future Enhancements)
* **Batch Processing:** Queue multiple audio files to be transcribed sequentially.
* **Timestamps:** Option to include timestamps in the generated transcript (e.g., `[00:01:23] Hello world`).
* **Export Formats:** Exporting to `.srt` or `.vtt` for subtitle use.

## 4. Non-Functional Requirements
* **Privacy & Connectivity:** The application must never make network requests during the transcription process. 100% offline capability is mandatory.
* **Readability:** Avalonia views must be kept small and strictly focused on presentation. Complex logic must be pushed down into injected C# services.
* **Error Handling:** The app must gracefully handle errors (e.g., unsupported file formats, corrupted models) and display user-friendly UI alerts.
* **Performance Constraints:** While raw performance is not the priority, the application UI must remain responsive. Heavy transcription workloads must be offloaded to a background `Task`.

## 5. UI/UX Requirements
* **Single Page Dashboard:** A clean, single-view layout consisting of:
  * **Configuration Section:** A model size dropdown (Tiny / Base / Small / Medium) with a cached-model indicator, and a "Select Audio File" button with the selected filename shown alongside.
  * **Transcript Area:** A large, styled text box to display the ongoing or finished transcript.
  * **Action Bar:** A "Transcribe" button, a "Cancel" button, a progress indicator (shared between model download and transcription), and an "Export" button.
* **State Management:** Buttons should be visually disabled contextually using Avalonia's binding system (e.g., "Transcribe" is disabled until an audio file is selected; "Export" is disabled until transcription is complete; "Cancel" is only active during a download or transcription).

## 6. Likely External Dependencies
* **UI Framework:** `Avalonia UI` with the MVVM Community Toolkit for view models and data binding.
* **Transcription Engine:** `Whisper.net` (binds to `whisper.cpp`), the standard choice for offline C# transcription.
* **Audio Processing:** `FFMpegCore` + system FFmpeg (must be installed separately). NAudio is not suitable as its format conversion pipeline has significant gaps on Linux and macOS. FFMpegCore provides reliable cross-platform `.mp3`/`.m4a` → 16kHz WAV conversion.
* **Model Download:** `Whisper.net`'s built-in `WhisperGgmlDownloader` — downloads GGML model files from Hugging Face on first use and caches them in the user's app-data folder (`%APPDATA%` / `~/.local/share`).

## 7. Out of Scope
* Real-time microphone dictation / live transcription.
* Mobile support (iOS/Android). This PRD is strictly for Desktop (Windows/macOS/Linux).
* Multi-user logins or cloud synchronization.
* Aggressive memory management or hyper-optimization using pointers/unsafe code.
* Auto-updating mechanism.
* Analytics, crash reporting, or telemetry.