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

        public override void Initialize()
        {
            if (!LiveMode)
            {
                throw new InvalidOperationException("Local live monitor only.");
            }

            SetTimeZone("Asia/Kolkata");
            SetAccountCurrency("INR");
            SetBrokerageModel(BrokerageName.Zerodha, AccountType.Margin);

            DefaultOrderProperties = new IndiaOrderProperties(
    Exchange.NSE, 
    IndiaOrderProperties.IndiaProductType.MIS
);

            // Raw tick/second aggregation
            var equity = AddEquity("IDEA", Resolution.Second, Market.India,
                fillForward: true, 
                dataNormalizationMode: DataNormalizationMode.Raw);

            _idea = equity.Symbol;
            SetBenchmark(_idea);

            Log($"[LOCAL INIT] Started live monitoring for {_idea} via Zerodha.");
        }

        public override void OnData(Slice slice)
        {
            // Verify security has valid market data
            if (!Securities.TryGetValue(_idea, out var security) || !security.HasData)
            {
                return;
            }

            decimal currentPrice = security.Price;

            // Bar volume for the 1-second period
            decimal barVolume = slice.Bars.TryGetValue(_idea, out var bar) ? bar.Volume : 0m;

            // Day's cumulative traded volume reported by Kite
            decimal dayVolume = security.Volume;

            // High-throughput local stdout
            Console.WriteLine($"[{Time:HH:mm:ss}] LTP: ₹{currentPrice:F2} | 1s-Vol: {barVolume:N0} | Day-Vol: {dayVolume:N0}");
        }
    }
}
