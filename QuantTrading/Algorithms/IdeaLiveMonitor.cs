using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Market;
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

        public void OnData(Ticks ticks)
        {
            if (!ticks.TryGetValue(_idea, out var tickList) || tickList.Count == 0 || Time < _nextLog) 
                return;

            _nextLog = Time.AddMinutes(1);

            // Fetch the final tick block element in the array safely
            var latestTick = tickList[tickList.Count - 1];

            // Explicitly reading Quantity as a decimal completely resolves the compiler mismatch
            decimal volume = latestTick.Quantity; 

            Log($"[TICK FEED] {Time:yyyy-MM-dd HH:mm:ss} IST | Ticks in Bundle = {tickList.Count} | Latest Price = {latestTick.Price} | Vol = {volume}");
        }
    }
}
