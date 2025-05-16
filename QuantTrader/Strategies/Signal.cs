using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Strategies
{
    /// <summary>
    /// 交易信号
    /// </summary>
    public class Signal
    {
        public string Symbol { get; set; }
        public SignalType Type { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }

        public override string ToString()
        {
            return $"Signal: {Type} {Quantity} {Symbol} @ {Price} - {Reason}";
        }
    }
}
