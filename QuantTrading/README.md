# QuantTrading — NuGet-based C# LEAN project

Created 2026-09-14. This standalone project builds Algorithms and Runner from NuGet packages; it has no project references to the old LEAN source checkout.

## Versions
- .NET 10
- QuantConnect LEAN and Zerodha packages: 2.5.18042 (current 2.5 stable line verified against the live NuGet feed)
- NetMQ 4.0.4.3; System.Security.Cryptography.Xml 10.0.12
- System.Drawing.Common 4.7.2: compatible security patch for the legacy transitive dependency; not the latest major release. A Windows-only major upgrade is unsuitable as an automatic Linux compatibility fix.
- packages.lock.json files pin the resolved dependency graph. Audit remains enabled.

## Run on this server
```bash
cd /root/QuantTrading
bash run-backtest.sh
```
Default data directory: Data inside this project. The script resolves it relative to its own location, so the project can be moved. Set LEAN_DATA_DIR only to intentionally use another dataset. The archive includes India sample equity data and LEAN market-hours/symbol-properties metadata copied from the existing checkout. The backtest does not require that checkout. .NET 10 and NuGet restore access (or a populated package cache) are still required.

## How it works
Algorithms/YesBankSimpleBacktest.cs buys 100 YESBANK shares once over 9–11 July 2019 and holds them to the end. It explicitly refuses live mode. Runner invokes the packaged LEAN launcher's entry point and uses its runtime dependency graph. Run from the script so configuration and result paths are explicit. LEAN's packaged examples may be transitive binary dependencies, but their source is not compiled here.

## Validation
Release build passed: zero errors; four repeated NU1903 warnings about the same DotNetZip package. The packaged launcher completed the YESBANK backtest successfully. Consult results/yesbank-validation on the server for the output. Missing quote/hourly benchmark files are limitations of the supplied sample data.

## Remaining dependency issue
The latest QuantConnect.Compression package still references DotNetZip 1.16.0. Its path-traversal advisory remains unresolved and visible. Moving to packages does not patch upstream code. The separate ZIP source fix made in the old checkout is NOT part of this NuGet project. Do not treat the project as security-cleared for production.

## Live trading
The Zerodha adapter package is restored, but no credentials, live configuration or live orders were used. Before live deployment, implement/review strategy risk limits, order reconciliation, daily authentication and the 15:14 IST exit. The current sample is buy-and-hold, not the final intraday strategy.

## Sources
- https://www.nuget.org/packages/QuantConnect.Lean/2.5.18042
- https://www.nuget.org/packages/QuantConnect.ZerodhaBrokerage/2.5.18042
- https://github.com/advisories/GHSA-xhg6-9j5j-w4vf

## IDEA history downloader
See HistoryDownloader/README.md for the official Kite SDK integration and download command. Current download is blocked by an invalid/expired Kite session; the downloader itself builds without warnings.

## Local data validation
On 2026-09-14, copied and SHA-256 verified 13 India sample/metadata files into Data; also included the interest-rate series and US map/factor metadata required during statistics generation. The run script was invoked from /tmp to check project-relative data resolution. Results remain under results/yesbank-TIMESTAMP.
