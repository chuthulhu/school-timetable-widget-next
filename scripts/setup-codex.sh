#!/usr/bin/env bash
# Source from the repository root to retain PATH in the current shell.
set -euo pipefail

if [[ "$(uname -s)" != Linux ]]; then
  echo "Codex setup requires Linux; use scripts/bootstrap-dev.ps1 on Windows." >&2
  exit 2
fi
stw_repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$stw_repo_root"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# Reuse a compatible SDK. If none resolves global.json, install the exact
# repository baseline with the official non-admin CI installer.
stw_sdk_dir="${STW_DOTNET_DIR:-$HOME/.dotnet}"
if [[ -x "$stw_sdk_dir/dotnet" ]]; then
  export DOTNET_ROOT="$stw_sdk_dir"
  export PATH="$stw_sdk_dir:$PATH"
fi
if ! dotnet --version >/dev/null 2>&1; then
  stw_installer="$(mktemp)"
  curl --fail --show-error --silent --location --retry 3 \
    https://dot.net/v1/dotnet-install.sh --output "$stw_installer"
  bash "$stw_installer" --jsonfile "$stw_repo_root/global.json" \
    --install-dir "$stw_sdk_dir" --no-path
  rm -- "$stw_installer"
  export DOTNET_ROOT="$stw_sdk_dir"
  export PATH="$stw_sdk_dir:$PATH"
fi
dotnet --version
dotnet restore src/SchoolTimetableWidget.Core/SchoolTimetableWidget.Core.csproj
dotnet build src/SchoolTimetableWidget.Core/SchoolTimetableWidget.Core.csproj --no-restore
echo "Core build complete. Full WPF tests require Windows; see docs/CLOUD-DEVELOPMENT.md."
