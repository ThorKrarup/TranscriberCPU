# Product Requirements Document (PRD)

**Project Name:** LocalNet Transcriber (Working Title)
**Platform:** Desktop Windows/macOS/Linux (.NET 10 via Avalonia UI)
**Primary User:** Single User (Personal Tool)
**Status:** Planning / PRD Phase

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
* **Model Loading:** The application can load a local, pre-downloaded transcription model (e.g., a Whisper `.bin` model) via the native file picker.
* **UI Feedback:** The user can clearly see the status of the application (e.g., "Idle", "Loading Model", "Transcribing...").
* **Text Output:** The transcribed text is displayed in a readable, scrollable text control within the Avalonia view.
* **Export:** The user can save the transcribed text to a local `.txt` file using native save dialogs.

### 3.2. Should Have
* **Progress Indicator:** A visual progress bar (HTML5/CSS) or percentage indicator during the transcription process.
* **Model Selection Memory:** The application remembers the path to the last used transcription model using a lightweight JSON settings file, preventing repetitive file picking.
* **Cancellation:** The user can safely click a "Cancel" button to stop a transcription in progress without crashing the application (utilizing `CancellationToken`).

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
  * **Configuration Section:** Buttons to "Select Model" and "Select Audio File", with text labels showing the currently selected file paths.
  * **Transcript Area:** A large, styled text box to display the ongoing or finished transcript.
  * **Action Bar:** A "Transcribe" button, a "Cancel" button, a progress indicator, and an "Export" button.
* **State Management:** Buttons should be visually disabled contextually using Avalonia's binding system (e.g., "Transcribe" is disabled until an audio file and model are selected; "Export" is disabled until transcription is complete).

## 6. Likely External Dependencies
* **UI Framework:** `Avalonia UI` with the MVVM Community Toolkit for view models and data binding.
* **Transcription Engine:** `Whisper.net` (binds to `whisper.cpp`), the standard choice for offline C# transcription.
* **Audio Processing:** `FFMpegCore` + `FFMpegCore.Binaries` (bundles platform-appropriate FFmpeg binaries — no external install required). NAudio is not suitable as its format conversion pipeline has significant gaps on Linux and macOS. FFMpegCore provides reliable cross-platform `.mp3`/`.m4a` → 16kHz WAV conversion.

## 7. Out of Scope
* Real-time microphone dictation / live transcription.
* Mobile support (iOS/Android). This PRD is strictly for Desktop (Windows/macOS/Linux).
* Multi-user logins or cloud synchronization.
* Aggressive memory management or hyper-optimization using pointers/unsafe code.
* Auto-updating mechanism.
* Analytics, crash reporting, or telemetry.