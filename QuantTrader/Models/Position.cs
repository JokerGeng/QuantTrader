using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    /// <summary>
    /// 持仓信息
    /// </summary>
    public class Position
    {
        public string Symbol { get; set; }
        public int Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal MarketValue => Quantity * CurrentPrice;
        public decimal UnrealizedPnL => Quantity * (CurrentPrice - AverageCost);
        public decimal UnrealizedPnLPercent => AverageCost == 0 ? 0 : Math.Round(UnrealizedPnL / (AverageCost * Quantity) * 100, 2);

        public bool IsLong => Quantity > 0;
        public bool IsShort => Quantity < 0;
        public bool IsFlat => Quantity == 0;

        public Position(string symbol, int quantity = 0, decimal averageCost = 0, decimal currentPrice = 0)
        {
            Symbol = symbol;
            Quantity = quantity;
            AverageCost = averageCost;
            CurrentPrice = currentPrice;
        }

        public void UpdatePrice(decimal newPrice)
        {
            CurrentPrice = newPrice;
        }
    }
}
