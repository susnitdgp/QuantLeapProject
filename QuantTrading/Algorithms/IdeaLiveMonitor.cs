using System;
using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class IdeaLiveMonitor : QCAlgorithm
    {
        private Symbol _idea;
        private DateTime _nextLog = DateTime.MinValue;

        public override void Initialize()
        {
            if (!LiveMode) throw new InvalidOperationException("Live monitor only");

            SetTimeZone("Asia/Kolkata");
            SetAccountCurrency("INR");
            SetBrokerageModel(BrokerageName.Zerodha, AccountType.Margin);

            // 1. Subscribe using Second resolution (reliably aggregated from Kite stream)
            _idea = AddEquity("IDEA", Resolution.Second, Market.India,
                fillForward: true, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;

            SetBenchmark(_idea);
            DefaultOrderProperties = new IndiaOrderProperties(exchange: Exchange.NSE);

            Log("[INIT] IDEA Live Monitor initialized successfully on Zerodha feed.");
        }

        public override void OnData(Slice slice)
        {
            // Throttle logging to once every 10 seconds
            if (Time < _nextLog) return;

            // Check TradeBar stream (primary for live Zerodha feed)
            if (slice.Bars.TryGetValue(_idea, out var bar))
            {
                _nextLog = Time.AddSeconds(10);
                Log($"[BAR FEED] {Time:yyyy-MM-dd HH:mm:ss} IST | Close/LTP = ₹{bar.Close} | Vol = {bar.Volume} | High = ₹{bar.High} | Low = ₹{bar.Low}");
            }
            // Check Raw Tick stream if available
            else if (slice.Ticks.TryGetValue(_idea, out var tickList) && tickList.Count > 0)
            {
                _nextLog = Time.AddSeconds(10);
                var latestTick = tickList[tickList.Count - 1];
                Log($"[TICK FEED] {Time:yyyy-MM-dd HH:mm:ss} IST | Ticks = {tickList.Count} | LTP = ₹{latestTick.Price} | Vol = {latestTick.Quantity}");
            }
            // Fallback to cache
            else if (Securities.ContainsKey(_idea) && Securities[_idea].Price > 0)
            {
                _nextLog = Time.AddSeconds(10);
                Log($"[SECURITIES CACHE] {Time:yyyy-MM-dd HH:mm:ss} IST | Cached LTP = ₹{Securities[_idea].Price}");
            }
        }
    }
}
