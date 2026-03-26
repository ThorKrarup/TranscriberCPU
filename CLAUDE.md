# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build all
dotnet build TranscriberCPU.sln

# Build individual projects
dotnet build LocalNetTranscriber.Core/LocalNetTranscriber.Core.csproj
dotnet build LocalNetTranscriber.Infrastructure/LocalNetTranscriber.Infrastructure.csproj
dotnet build LocalNetTranscriber.UI/LocalNetTranscriber.UI.csproj

# Run
dotnet run --project LocalNetTranscriber.UI/LocalNetTranscriber.UI.csproj

# Test
dotnet test TranscriberCPU.Tests/TranscriberCPU.Tests.csproj
```

## Project Structure

.NET 10.0 solution with four projects. The solution file (`TranscriberCPU.sln`) is at the root.

- `LocalNetTranscriber.Core/` — class library; models, interfaces, exceptions; zero external NuGet deps
- `LocalNetTranscriber.Infrastructure/` — class library; implements Core interfaces (audio processing, transcription)
- `LocalNetTranscriber.UI/` — Avalonia MVVM executable (`OutputType=WinExe`); references Core and Infrastructure
- `TranscriberCPU.Tests/` — xUnit test project; references Core and Infrastructure

Nullable reference types and implicit usings are enabled.