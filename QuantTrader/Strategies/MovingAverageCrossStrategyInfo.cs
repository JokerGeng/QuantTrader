using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public class MovingAverageCrossStrategyInfo : StrategyInfoBase
    {
        public MovingAverageCrossStrategyInfo() : base()
        {
            Name = "Moving Average Cross";
            Description = "Buy when fast MA crosses above slow MA, sell when fast MA crosses below slow MA";

            Parameters = new List<StrategyParameter>()
            {
                new StrategyParameter(){Name="FastPeriod",Value=5},
                new StrategyParameter(){Name="SlowPeriod",Value=20},
                new StrategyParameter(){Name="Quantity",Value=100},
                new StrategyParameter(){Name="CandlestickPeriod",Value=TimeSpan.FromMinutes(5)},
                new StrategyParameter(){Name="MaxPositionValue",Value="100000m"},
            };
        }
    }
}
