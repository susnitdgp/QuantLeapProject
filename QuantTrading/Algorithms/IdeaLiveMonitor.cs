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
            if (Securities.TryGetValue(_idea, out var security))
            {
                // Zerodha populates day volume on the Security object from REST/Quote snapshots
                decimal totalDayVolume = security.Volume;
                decimal currentLtp = security.Price;
        
                Log($"[SECURITY SNAPSHOT] {Time:HH:mm:ss} | Price: ₹{currentLtp} | Day Volume: {totalDayVolume:N0}");
            }
        }
    }
}
