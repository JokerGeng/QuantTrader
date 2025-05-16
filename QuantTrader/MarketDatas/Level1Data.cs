using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// Level1行情数据
    /// </summary>
    public class Level1Data : MarketData
    {
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Volume { get; set; }
        public decimal Turnover { get; set; }
        public decimal BidPrice1 { get; set; }
        public decimal BidVolume1 { get; set; }
        public decimal AskPrice1 { get; set; }
        public decimal AskVolume1 { get; set; }
        public decimal PreClose { get; set; }

        public decimal Change => LastPrice - PreClose;
        public decimal ChangePercent => PreClose == 0 ? 0 : Math.Round(Change / PreClose * 100, 2);
    }
}
