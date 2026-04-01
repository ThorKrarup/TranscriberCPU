#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p docs/audit

# Ensure dotnet is on PATH (common install location on Linux)
export PATH="$PATH:$HOME/.dotnet"

dotnet restore TranscriberCPU.sln

# Full dependency list (direct + transitive)
dotnet list TranscriberCPU.sln package --include-transitive \
  > docs/audit/dependencies.txt

# Vulnerability scan (all severities including transitive)
dotnet list TranscriberCPU.sln package --vulnerable --include-transitive \
  > docs/audit/vulnerability-report.txt 2>&1 || true

echo "Audit complete. Results saved to docs/audit/"
cat docs/audit/vulnerability-report.txt
