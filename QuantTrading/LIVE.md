# Zerodha live connection setup

The IDEA tick monitor uses the installed Zerodha brokerage adapter, INR, NSE and MIS. It contains no strategy entry/exit orders. Existing account positions are real; this is not a paper brokerage. Do not use this monitor to manage positions or enforce the 15:14 exit.

Credentials are read from config/kite.json (api_key/access_token). Environment values override the file values. Credentials are merged into an owner-only temporary configuration removed when the launcher exits normally; they are never supplied on the command line. Live output is private under live-results/ and ignored by Git. Existing backtest results remain tracked.

Build and configuration check (does not authenticate or connect):
```bash
cd /root/QuantTrading
python3 run-live-monitor.py
```

Explicitly connect the monitor:
```bash
python3 run-live-monitor.py --start
```
Stop with Ctrl+C. No service or automatic restart is installed. This does not liquidate any broker position.

Before enabling strategy orders, define the strategy, share quantity, stop-loss and daily loss limit. Implement pending-order tracking, rejection/partial-fill handling, stale-feed entry blocking, restart reconciliation and 15:14 IST entry cutoff/cancel/exit with fill verification. The YESBANK sample remains backtest-only. The DotNetZip advisory remains unresolved.

Reference: https://github.com/QuantConnect/Lean.Brokerages.Zerodha
