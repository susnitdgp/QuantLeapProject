using System;
using QuantConnect.Data;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class YesBankSimpleBacktest : QCAlgorithm
    {
        private Symbol _symbol;
        private bool _submitted;
        public override void Initialize()
        {
            if (LiveMode) throw new InvalidOperationException("Backtest only");
            SetAccountCurrency("INR");
            SetTimeZone("Asia/Kolkata");
            
            SetStartDate(2019, 7, 9);
            SetEndDate(2019, 7, 11);
            SetCash(100000);

            
            var security = AddEquity("YESBANK", Resolution.Minute, Market.India, fillForward: false, dataNormalizationMode: DataNormalizationMode.Raw);
            _symbol = security.Symbol;
            SetBenchmark(_symbol);
            DefaultOrderProperties = new IndiaOrderProperties(exchange: Exchange.NSE);
        }
        public override void OnData(Slice slice)
        {
            if (_submitted || !slice.Bars.ContainsKey(_symbol)) return;
            _submitted = true;
            MarketOrder(_symbol, 100, tag: "Sample buy and hold");
        }
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }
        public override void OnEndOfAlgorithm()
        {
            Log($"FINAL quantity={Portfolio[_symbol].Quantity} price={Securities[_symbol].Price} equity={Portfolio.TotalPortfolioValue}");
        }
    }
}
