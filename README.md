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
3. Click **Transcribe**. Progress is shown in the status bar.
4. When done, click **Export** to save the transcript as a `.txt` file.

The **Cancel** button safely aborts an in-progress download or transcription.

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
