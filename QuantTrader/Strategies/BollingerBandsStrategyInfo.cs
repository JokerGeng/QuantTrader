using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public class BollingerBandsStrategyInfo : StrategyInfoBase
    {
        public BollingerBandsStrategyInfo(string id, IDataRepository dataRepository) : base(id, dataRepository)
        {
            Name = "Bollinger Bands Strategy";
            Description = "Buy when price touches the lower band, sell when price touches the upper band";

            Parameters = new List<StrategyParameter>()
            {
                new StrategyParameter(){Name="Period",Value=20},
                new StrategyParameter(){Name="Multiplier",Value=2.0m},
                new StrategyParameter(){Name="Quantity",Value=100},
                new StrategyParameter(){Name="CandlestickPeriod",Value=TimeSpan.FromMinutes(5)},
                new StrategyParameter(){Name="MaxPositionValue",Value=100000m},
                new StrategyParameter(){Name="ExitMiddleBand",Value=true,Description="是否在价格回归到中轨时平仓"}
            };
        }
    }
}
