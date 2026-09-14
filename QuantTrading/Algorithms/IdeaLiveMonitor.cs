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
            Log("IDEA live monitor initialized. Strategy order submission is not implemented.");
        }
        public override void OnData(Slice slice)
        {
            if (!slice.Ticks.TryGetValue(_idea, out var ticks) || ticks.Count == 0 || Time < _nextLog) return;
            _nextLog = Time.AddMinutes(1);
            Log($"IDEA feed {Time:yyyy-MM-dd HH:mm:ss} IST; ticks={ticks.Count}; price={Securities[_idea].Price}");
        }
    }
}
