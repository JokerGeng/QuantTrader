using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public class Account
    {
        public string AccountId { get; set; }
        public decimal Cash { get; set; }
        public decimal TotalAssetValue { get; set; }
        public decimal MarginUsed { get; set; }
        public decimal MarginAvailable { get; set; }
        public Dictionary<string, Position> Positions { get; private set; }

        public Account(string accountId, decimal initialCash = 100000)
        {
            AccountId = accountId;
            Cash = initialCash;
            TotalAssetValue = initialCash;
            Positions = new Dictionary<string, Position>();
        }

        public void UpdatePositions(IEnumerable<Position> positions)
        {
            Positions.Clear();
            foreach (var position in positions)
            {
                Positions[position.Symbol] = position;
            }

            // 更新总资产价值
            TotalAssetValue = Cash + Positions.Values.Sum(p => p.MarketValue);
        }

        public void UpdatePosition(Position position)
        {
            Positions[position.Symbol] = position;
            TotalAssetValue = Cash + Positions.Values.Sum(p => p.MarketValue);
        }
    }
}
