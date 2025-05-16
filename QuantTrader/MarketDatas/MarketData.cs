using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    public abstract class MarketData
    {
        public string Symbol { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
