# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build TranscriberCPU/TranscriberCPU.csproj

# Run
dotnet run --project TranscriberCPU/TranscriberCPU.csproj

# Publish
dotnet publish TranscriberCPU/TranscriberCPU.csproj -c Release

# Docker
docker compose up --build
```

## Project Structure

Single-project .NET 10.0 console application (`OutputType=Exe`). All application code lives in `TranscriberCPU/`. The solution file (`TranscriberCPU.sln`) is at the root.

Nullable reference types and implicit usings are enabled.

The app is deployed as a Linux Docker container (see `TranscriberCPU/Dockerfile` and `compose.yaml`).