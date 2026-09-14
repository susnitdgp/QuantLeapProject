#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
dotnet build "$project_root/Runner/QuantTrading.Runner.csproj" -c Release -p:RestoreLockedMode=true
exec dotnet "$project_root/Runner/bin/Release/net10.0/QuantTrading.Runner.dll" --config "$project_root/config/backtest.json" --data-folder "${LEAN_DATA_DIR:-$project_root/Data}" --results-destination-folder "$project_root/results/yesbank-$(date -u +%Y%m%dT%H%M%SZ)"
