using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// K线数据
    /// </summary>
    public class Candlestick : MarketData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public TimeSpan Period { get; set; }

        public bool IsUp => Close >= Open;
        public decimal Range => High - Low;
        public decimal Body => Math.Abs(Close - Open);
    }
}
