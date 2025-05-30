using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using System.Xml.Linq;

namespace QuantTrader.Strategies
{
    public class RSIStrategyInfo : StrategyInfoBase
    {
        public RSIStrategyInfo(string id, IDataRepository dataRepository) : base(id, dataRepository)
        {
            Name = "RSI Strategy";
            Description = "Buy when RSI is below oversold level, sell when RSI is above overbought level";

            Parameters = new List<StrategyParameter>()
            {
                new StrategyParameter(){Name="RSIPeriod",Value=14},
                new StrategyParameter(){Name="OversoldLevel",Value=30},
                new StrategyParameter(){Name="OverboughtLevel",Value=70},
                new StrategyParameter(){Name="Quantity",Value=100},
                new StrategyParameter(){Name="CandlestickPeriod",Value=TimeSpan.FromMinutes(5)},
                new StrategyParameter(){Name="MaxPositionValue",Value=100000m},
            };
        }
    }
}
