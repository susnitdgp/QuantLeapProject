using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Brokerages;

namespace QuantConnect.Algorithm.CSharp
{
    public class GoswamiMovingAverage : QCAlgorithm
    {
        private Symbol _reliance;
        private ExponentialMovingAverage _fastEma;
        private ExponentialMovingAverage _slowEma;

        public override void Initialize()
        {
            // 1. Set dates (used for warm-up or backtesting fallback)
            SetStartDate(2026, 1, 1);
            SetCash(500000); // 5 Lakhs INR

            // 2. Set the Brokerage Model to Zerodha (Sets structural margin rules)
            SetBrokerageModel(BrokerageName.Zerodha, AccountType.Margin);

            // 3. Add NSE Stock (Explicitly passing Market.India)
            _reliance = AddEquity("RELIANCE", Resolution.Minute, Market.India).Symbol;

            // 4. Set up Indicators
            _fastEma = EMA(_reliance, 5, Resolution.Minute);
            _slowEma = EMA(_reliance, 20, Resolution.Minute);

            // Warm up indicators so they are ready the instant live trading starts
            SetWarmUp(20, Resolution.Minute);
        }

        public override void OnData(Slice slice)
        {
            // Avoid trading during warm-up phase or if indicators aren't ready
            if (IsWarmingUp || !_fastEma.IsReady || !_slowEma.IsReady) return;

            // Ensure price bar exists for RELIANCE in the current slice
            if (!slice.Bars.ContainsKey(_reliance)) return;

            var holdings = Portfolio[_reliance].Quantity;

            // Buy Condition: Fast EMA crosses above Slow EMA
            if (_fastEma > _slowEma && holdings <= 0)
            {
                // Go long (Liquidates short position first if any exists)
                SetHoldings(_reliance, 0.5); 
                Log($"[BUY ORDER] Fast EMA ({_fastEma:F2}) > Slow EMA ({_slowEma:F2}). Executed at {Time}");
            }
            // Sell / Square off Condition: Fast EMA crosses below Slow EMA
            else if (_fastEma < _slowEma && holdings > 0)
            {
                // Liquidate active shares
                Liquidate(_reliance);
                Log($"[SQUARE OFF] Fast EMA ({_fastEma:F2}) < Slow EMA ({_slowEma:F2}). Executed at {Time}");
            }
        }
    }
}
