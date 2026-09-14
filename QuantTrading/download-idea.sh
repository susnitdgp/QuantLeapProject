#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
dotnet build "$project_root/HistoryDownloader/QuantTrading.HistoryDownloader.csproj" -c Release -p:RestoreLockedMode=true
credential_args=()
has_credentials=false
for arg in "$@"; do
    if [[ "$arg" == "--credentials" ]]; then
        has_credentials=true
        break
    fi
done
if [[ "$has_credentials" == false && -f "$project_root/config/kite.json" ]]; then
    credential_args=(--credentials "$project_root/config/kite.json")
fi
exec dotnet "$project_root/HistoryDownloader/bin/Release/net10.0/QuantTrading.HistoryDownloader.dll" --output "$project_root/downloads" "${credential_args[@]}" "$@"
