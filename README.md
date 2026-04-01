# LocalNet Transcriber

A desktop application that transcribes audio files locally using [Whisper](https://github.com/openai/whisper) — no cloud, no API keys, no data leaves your machine.

Built with .NET 10, Avalonia UI, and [Whisper.net](https://github.com/sandrohanea/whisper.net).

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [FFmpeg](https://ffmpeg.org/download.html) available on your `PATH`

**Install FFmpeg:**

| Platform | Command |
|----------|---------|
| Linux (apt) | `sudo apt install ffmpeg` |
| macOS (Homebrew) | `brew install ffmpeg` |
| Windows (winget) | `winget install ffmpeg` |

## Run

```bash
git clone <repo-url>
cd TranscriberCPU
dotnet run --project LocalNetTranscriber.UI/LocalNetTranscriber.UI.csproj
```

## Usage

1. **Select a Whisper model** from the dropdown (Tiny / Base / Small / Medium). The first time you use a model it will be downloaded automatically from Hugging Face and cached locally.
2. **Select an audio file** — supported formats: mp3, m4a, wav, ogg, flac, aac, wma, opus.
3. **Optionally enable Speaker Diarization.** Check "Enable Speaker Diarization" to identify who spoke when. Set "Number of Speakers" to the known count or leave it at 0 for auto-detect. Diarization models (~25 MB) are downloaded automatically on first use.
4. Click **Transcribe**. Progress is shown in the status bar.
5. **Optionally rename speakers.** After a diarized transcription the "Speaker Labels" panel appears above the transcript. Type a custom name (e.g. "Alice") next to each speaker ID — the transcript preview updates live. Leave a field blank to keep the auto-generated label.
6. **Select an export format** (Plain Text or Markdown) from the dropdown next to the Export button.
7. Click **Export** to save the transcript. Custom speaker names are applied in the saved file.

The **Cancel** button safely aborts an in-progress download, transcription, or diarization pass.

## Model sizes

| Model | Size | Speed | Accuracy |
|-------|------|-------|----------|
| Tiny | ~75 MB | Fastest | Lower |
| Base | ~142 MB | Fast | Good |
| Small | ~466 MB | Moderate | Better |
| Medium | ~1.5 GB | Slow | Best |

Models are cached in `%APPDATA%/LocalNetTranscriber/models/` (Windows) or `~/.config/LocalNetTranscriber/models/` (Linux/macOS).

## Build

```bash
dotnet build TranscriberCPU.sln
dotnet test TranscriberCPU.Tests/TranscriberCPU.Tests.csproj
```

## Dependency security

NuGetAudit is enabled solution-wide (`Directory.Build.props`). Every `dotnet restore` checks all packages (direct and transitive) against the GitHub Advisory Database. Moderate, High, and Critical vulnerabilities fail the build; Low severity is reported as a warning.

To run a manual audit and save the results:

```bash
./scripts/audit.sh
```

Results are saved to `docs/audit/`:
- `dependencies.txt` — full package list per project
- `vulnerability-report.txt` — latest audit output
