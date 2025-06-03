using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public abstract class StrategyInfoBase : IStrategyInfo
    {

        public string Name { get; protected set; }

        public string Description { get; protected set; }

        public List<StrategyParameter> Parameters { get; protected set; }

        protected StrategyInfoBase()
        {
        }
    }
}
