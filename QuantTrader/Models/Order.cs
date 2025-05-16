using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public class Order
    {
        public string OrderId { get; set; }
        public string Symbol { get; set; }
        public OrderDirection Direction { get; set; }
        public OrderType Type { get; set; }
        public decimal Price { get; set; }
        public decimal StopPrice { get; set; }
        public int Quantity { get; set; }
        public int FilledQuantity { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public string StrategyId { get; set; }
        public string Message { get; set; }

        public decimal AverageFilledPrice { get; set; }
        public bool IsFilled => Status == OrderStatus.Filled;
        public bool IsActive => Status == OrderStatus.Created ||
                               Status == OrderStatus.Submitted ||
                               Status == OrderStatus.PartiallyFilled;

        public override string ToString()
        {
            return $"Order[{OrderId}]: {Direction} {Quantity} {Symbol} @ {Price}, Status: {Status}";
        }
    }
}
