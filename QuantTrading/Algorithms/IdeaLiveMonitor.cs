using System;
using QuantConnect.Data;
using QuantConnect.Orders;
using QuantConnect.Brokerages;

namespace QuantConnect.Algorithm.CSharp
{
    // Connection validation only: no strategy orders or scheduled liquidation.
    public class IdeaLiveMonitor : QCAlgorithm
    {
        private Symbol _idea;
        private DateTime _nextLog;
        public override void Initialize()
        {
            if (!LiveMode) throw new InvalidOperationException("Live monitor only");
            SetTimeZone("Asia/Kolkata");
            SetAccountCurrency("INR");
            SetBrokerageModel(BrokerageName.Zerodha, AccountType.Margin);
            _idea = AddEquity("IDEA", Resolution.Tick, Market.India,
                fillForward: false, dataNormalizationMode: DataNormalizationMode.Raw).Symbol;
            SetBenchmark(_idea);
            DefaultOrderProperties = new IndiaOrderProperties(exchange: Exchange.NSE);
            Log("IDEA Live Monitor initialized. Strategy order submission is not implemented.");
        }
        /*
        public override void OnData(Slice slice)
        {
            if (!slice.Ticks.TryGetValue(_idea, out var ticks) || ticks.Count == 0 || Time < _nextLog) return;
            _nextLog = Time.AddMinutes(1);
            Log($"IDEA feed {Time:yyyy-MM-dd HH:mm:ss} IST; ticks={ticks.Count}; price={Securities[_idea].Price}");
        }*/

        // Change your method signature to target the Ticks collection directly
        public void OnData(Ticks ticks)
        {
            // 1. Verify data exists for this symbol and throttle the logging output
            if (!ticks.TryGetValue(_idea, out var tickList) || tickList.Count == 0 || Time < _nextLog) 
                return;

            _nextLog = Time.AddMinutes(1);

            // 2. Loop through every tick bundled in this timeframe snapshot
            foreach (var tick in tickList)
            {
                // Access specific tick fields natively:
                decimal price = tick.Price;
                long volume = tick.Quantity; 
                TickType type = tick.TickType; // Trade or Quote

                Log($"[TICK EVENT] Time: {tick.Time:HH:mm:ss} | Price: {price} | Vol: {volume} | Type: {type}");
            }

            // Print summary metrics to the logging file
            Log($"IDEA feed updated at {Time:yyyy-MM-dd HH:mm:ss} IST; ticks collected={tickList.Count}; current price={Securities[_idea].Price}");
        }


    }
}
