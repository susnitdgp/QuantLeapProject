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
        private bool _orderSubmitted = false;

        public override void Initialize()
        {
            if (!LiveMode)
            {
                throw new InvalidOperationException("Local live monitor only.");
            }

            SetTimeZone("Asia/Kolkata");
            SetAccountCurrency("INR");
            SetCash(500);
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
            // Wait until security data is ready
            if (!Securities.TryGetValue(_idea, out var security) || !security.HasData) 
                return;
        
            // Buy 1 share once if no position is currently held
            if (!_orderSubmitted && !Portfolio[_idea].Invested)
            {
                var ticket = MarketOrder(_idea, 1);
                _orderSubmitted = true;
        
                Log($"[ORDER SENT] MarketOrder submitted for 1 qty {_idea}. Order ID: {ticket.OrderId}");
            }
        }
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                Log($"[ORDER FILLED] {orderEvent.Symbol} filled {orderEvent.FillQuantity} shares @ ₹{orderEvent.FillPrice:F2}");
            }
        }
    }
}
