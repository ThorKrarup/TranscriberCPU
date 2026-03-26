# Phase 1 — Solution Restructure & Core Foundation

## 1. Remove old projects

- [x] Delete `TranscriberCPU/` directory (console scaffold — `TranscriberCPU.csproj`, `Program.cs`, `Dockerfile`, etc.)
- [x] Delete `TranscriberCPU.Tests/` directory (will be recreated referencing Core)

## 2. Create `LocalNetTranscriber.Core` (class library)

- [x] Create `LocalNetTranscriber.Core/LocalNetTranscriber.Core.csproj`
  - `<OutputType>` omitted (class library default)
  - `net10.0`, nullable enabled, implicit usings enabled
  - **No `<PackageReference>` entries** (zero external NuGet deps)

- [x] Create `LocalNetTranscriber.Core/Models/TranscriptionResult.cs`
  - Properties: `string Text`, `TimeSpan Duration`, `IReadOnlyList<TranscriptionSegment> Segments` (segment = start/end/text)

- [x] Create `LocalNetTranscriber.Core/Models/AudioFileContext.cs`
  - Properties: `string FilePath`, `string FileName`, `string Extension`

- [x] Create `LocalNetTranscriber.Core/Interfaces/ITranscriptionService.cs`
  - `Task<TranscriptionResult> TranscribeAsync(string wavFilePath, IProgress<double> progress, CancellationToken ct)`

- [x] Create `LocalNetTranscriber.Core/Interfaces/IAudioPreprocessor.cs`
  - `Task<string> ConvertToWavAsync(string inputFilePath, CancellationToken ct)` — returns temp WAV path

- [x] Create `LocalNetTranscriber.Core/Interfaces/IFilePickerService.cs`
  - `Task<string?> PickFileAsync(string title, IEnumerable<string> allowedExtensions)`

- [x] Create `LocalNetTranscriber.Core/Interfaces/IFileSaverService.cs`
  - `Task<string?> PickSavePathAsync(string title, string defaultFileName, string extension)`

- [x] Create `LocalNetTranscriber.Core/Interfaces/ISettingsService.cs`
  - `Task<AppSettings> LoadAsync()`
  - `Task SaveAsync(AppSettings settings)`
  - Define `AppSettings` record inline or in `Models/` — holds `LastModelPath`

- [x] Create `LocalNetTranscriber.Core/Exceptions/UnsupportedAudioFormatException.cs`
  - Inherits `Exception`; constructor accepts `string extension`

- [x] Create `LocalNetTranscriber.Core/Exceptions/ModelLoadException.cs`
  - Inherits `Exception`; constructor accepts `string modelPath` and optional `Exception? innerException`

## 3. Create `LocalNetTranscriber.Infrastructure` (class library, stub)

- [x] Create `LocalNetTranscriber.Infrastructure/LocalNetTranscriber.Infrastructure.csproj`
  - References `LocalNetTranscriber.Core`
  - No implementation files yet (implementations come in Phase 2)

## 4. Create `LocalNetTranscriber.UI` (executable, stub)

- [x] Create `LocalNetTranscriber.UI/LocalNetTranscriber.UI.csproj`
  - `<OutputType>WinExe</OutputType>` (Avalonia convention)
  - References `LocalNetTranscriber.Core` and `LocalNetTranscriber.Infrastructure`
  - No implementation files yet (implementation comes in Phase 3)

## 5. Recreate test project

- [x] Create `LocalNetTranscriber.Tests/LocalNetTranscriber.Tests.csproj`
  - Same xUnit stack as before (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`)
  - References `LocalNetTranscriber.Core`
  - Note: kept as `TranscriberCPU.Tests` name but with correct references
- [x] Add a placeholder `CoreSmokeTests.cs` with one passing test to confirm the build works end-to-end

## 6. Restructure the solution file

- [x] Edit `TranscriberCPU.sln`:
  - Remove entries for `TranscriberCPU` and `TranscriberCPU.Tests`
  - Add entries for `LocalNetTranscriber.Core`, `LocalNetTranscriber.Infrastructure`, `LocalNetTranscriber.UI`, `LocalNetTranscriber.Tests`
  - Add all four projects to `GlobalSection(ProjectConfigurationPlatforms)` for `Debug|Any CPU` and `Release|Any CPU`

## 7. Update `CLAUDE.md`

- [x] Update build/run/test commands to use the new project names
- [x] Update project structure description

## 8. Verify

- [x] `dotnet build LocalNetTranscriber.Core/LocalNetTranscriber.Core.csproj` — clean
- [x] `dotnet build LocalNetTranscriber.Infrastructure/LocalNetTranscriber.Infrastructure.csproj` — clean
- [x] `dotnet build LocalNetTranscriber.UI/LocalNetTranscriber.UI.csproj` — clean
- [x] `dotnet test TranscriberCPU.Tests/TranscriberCPU.Tests.csproj` — 1 test passes
- [x] `dotnet build TranscriberCPU.sln` — all projects build from solution root