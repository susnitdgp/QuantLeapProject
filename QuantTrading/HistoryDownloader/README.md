# IDEA historical downloader

Uses the official Tech.Zerodha.KiteConnect 5.2.1 NuGet library in a separate .NET 10 executable. It calls only instrument-list and historical-data endpoints; it does not submit orders.

## Run on the server
```bash
cd /root/QuantTrading
bash download-idea.sh --from 2026-08-14 --to 2026-09-13
```
Edit `/root/QuantTrading/config/kite.json` and fill in `api_key` and `access_token`. The script automatically loads this file. On a fresh clone, create it with:
```bash
(umask 077; cp -n config/kite.example.json config/kite.json)
nano config/kite.json
```
The private file is ignored by Git; only the blank `kite.example.json` template is tracked. An explicit `--credentials PATH` overrides the default file. `KITE_API_KEY` and `KITE_ACCESS_TOKEN` environment values take precedence over file values. The downloader also accepts `zerodha-api-key` and `zerodha-access-token` field names. The API secret is not needed for historical requests with an existing access token.

Without dates, the request starts one calendar month before today in Asia/Kolkata and ends yesterday. Requests are made in non-overlapping seven-day windows, paced 400 ms apart. A failed request stops the run; no completed dataset is reported. Renew an invalid/expired session token through your existing Kite login procedure and rerun. Historical-data entitlement is required.

## Output
A unique directory under /root/QuantTrading/downloads contains:
- IDEA-minute.csv: chronological OHLCV candles with explicit +05:30 timestamps.
- manifest.json: instrument token, requested range, retrieval time, row counts per date, dates without candles and CSV SHA-256.
- lean/equity/india/minute/idea/YYYYMMDD_trade.zip: LEAN minute TradeBar files, timestamps in milliseconds from India midnight and prices scaled by 10,000.

The export does not insert synthetic candles, forward-fill, invent corporate-action factors or certify exchange-calendar completeness. It does not alter the existing LEAN Data directory. To backtest, integrate the trade files with the appropriate LEAN India market metadata and confirm the intended price normalization. Minute data cannot reproduce historical ticks or depth.

## Validation and current status (2026-09-14)
Release build passed with zero warnings and zero errors. NSE:IDEA resolved uniquely to instrument token 3677697. The historical request for 2026-08-14 through 2026-09-13 failed with TokenException using the existing server credentials. Both known LEAN configurations contained the same credential pair. No historical candles have been downloaded; output export remains unverified against a successful broker response.

## Reference
https://github.com/zerodha/dotnetkiteconnect
https://kite.trade/docs/connect/v3/historical/
