#!/usr/bin/env bash
# Offline Windows-x64 cross-build. SDK and a flat NuGet feed must be present.
set -euo pipefail
sharew_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
sharew_tools="${SHAREW_BUILD_TOOLS:-$sharew_root/../.build-tools}"
export DOTNET_ROOT="$sharew_tools/dotnet"
export DOTNET_CLI_HOME="$sharew_tools/cli-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export DOTNET_CLI_USE_MSBUILD_SERVER=0
export MSBUILDDISABLENODEREUSE=1
export XDG_RUNTIME_DIR="$sharew_tools/runtime"
export XDG_DATA_HOME="$sharew_tools/xdg-data"
export XDG_CACHE_HOME="$sharew_tools/xdg-cache"
export XDG_CONFIG_HOME="$sharew_tools/xdg-config"
export NUGET_PACKAGES="$sharew_tools/nuget-cache"
export NUGET_SCRATCH="$sharew_tools/nuget-scratch"
mkdir -p "$DOTNET_CLI_HOME" "$XDG_RUNTIME_DIR" "$XDG_DATA_HOME" "$XDG_CACHE_HOME" "$XDG_CONFIG_HOME" "$NUGET_PACKAGES" "$NUGET_SCRATCH" "$sharew_root/artifacts"
chmod 700 "$XDG_RUNTIME_DIR"
cd -- "$sharew_root"
python3 scripts/verify-source.py
test -x "$DOTNET_ROOT/dotnet" || { printf 'Missing Linux .NET SDK: %s\n' "$DOTNET_ROOT" >&2; exit 1; }
test -d "$sharew_tools/nuget-feed" || { printf 'Missing offline NuGet feed.\n' >&2; exit 1; }

# The source is selected explicitly; no user-wide NuGet configuration is changed.
sharew_flags=(-p:Platform=x64 -p:EnableWindowsTargeting=true -p:RestoreEnablePackagePruning=false -p:NuGetAudit=false -p:UsedAvaloniaProducts= -m:1 -nr:false)
"$DOTNET_ROOT/dotnet" restore src/shareW/shareW.csproj --source "$sharew_tools/nuget-feed" --packages "$NUGET_PACKAGES" --disable-build-servers --disable-parallel -r win-x64 -p:SelfContained=true "${sharew_flags[@]}"
"$DOTNET_ROOT/dotnet" publish src/shareW/shareW.csproj --no-restore -c Release -r win-x64 --self-contained true -p:UseSharedCompilation=false "${sharew_flags[@]}" -o artifacts/shareW-win-x64
# Native debug symbols are optional and substantially enlarge the portable.
rm -f -- artifacts/shareW-win-x64/libSkiaSharp.pdb artifacts/shareW-win-x64/libHarfBuzzSharp.pdb
printf '\nWindows files generated in artifacts/shareW-win-x64. Windows runtime tests remain necessary.\n'
