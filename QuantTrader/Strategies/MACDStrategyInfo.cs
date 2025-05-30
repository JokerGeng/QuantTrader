using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public class MACDStrategyInfo : StrategyInfoBase
    {
        public MACDStrategyInfo(string id) : base(id)
        {
            Name = "MACD Strategy";
            Description = "Buy when MACD histogram crosses above zero, sell when it crosses below zero";

            Parameters = new List<StrategyParameter>()
            {
                new StrategyParameter(){Name="FastPeriod",Value=12},
                new StrategyParameter(){Name="SlowPeriod",Value=26},
                new StrategyParameter(){Name="SignalPeriod",Value=9},
                new StrategyParameter(){Name="Quantity",Value=100},
                new StrategyParameter(){Name="CandlestickPeriod",Value=TimeSpan.FromMinutes(5)},
                new StrategyParameter(){Name="MaxPositionValue",Value=100000m},
                new StrategyParameter(){Name="UseHistogramSignal",Value=true,Description="是否使用柱状图交叉信号，否则使用MACD与信号线交叉"}
            };
        }
    }
}
