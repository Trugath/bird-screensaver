#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
dotnet build "$root/installer/BirdScreensaver.Installer.wixproj" -c Release
echo "MSI: $root/dist/BirdScreensaver-0.1.0-win-x64.msi"
