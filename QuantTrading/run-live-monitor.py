#!/usr/bin/env python3
"""Build/check by default; --start explicitly launches the live monitor."""
import argparse, json, os, subprocess, tempfile
from pathlib import Path
from datetime import datetime, timezone

root = Path(__file__).resolve().parent
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--start', action='store_true')
args = parser.parse_args()
try:
    credentials = json.loads((root / 'config/kite.json').read_text())
    key = os.environ.get('KITE_API_KEY') or credentials.get('api_key') or credentials.get('zerodha-api-key')
    token = os.environ.get('KITE_ACCESS_TOKEN') or credentials.get('access_token') or credentials.get('zerodha-access-token')
    if not key or not token:
        raise ValueError('missing credentials')
except (OSError, ValueError):
    raise SystemExit('Supply api_key and access_token in config/kite.json; credentials are never printed.')
config = json.loads((root / 'config/zerodha-monitor.json').read_text())
if config['algorithm-type-name'] != 'IdeaLiveMonitor':
    raise SystemExit('This launcher is only for IdeaLiveMonitor.')
subprocess.run(['dotnet', 'build', str(root / 'Runner/QuantTrading.Runner.csproj'), '-c', 'Release', '-p:RestoreLockedMode=false'], check=True)
print('Build/config check passed. Credentials present; broker authentication not checked.', flush=True)
if not args.start:
    print('No live process started. Use --start to connect the IDEA monitor.')
    raise SystemExit(0)
config['zerodha-api-key'] = key
config['zerodha-access-token'] = token
config['data-folder'] = str(root / 'Data')
output = root / 'live-results' / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S%fZ')
output.mkdir(parents=True, mode=0o700)
# Restrict runtime files and logs; never put credentials in command arguments.
os.umask(0o077)
with tempfile.TemporaryDirectory(prefix='quanttrading-live-') as tmp:
    path = Path(tmp) / 'config.json'
    path.write_text(json.dumps(config))
    command = ['dotnet', str(root / 'Runner/bin/Release/net10.0/QuantTrading.Runner.dll'), '--config', str(path), '--results-destination-folder', str(output)]
    try:
        result = subprocess.run(command, cwd=root)
        raise SystemExit(result.returncode)
    except KeyboardInterrupt:
        raise SystemExit(130)
