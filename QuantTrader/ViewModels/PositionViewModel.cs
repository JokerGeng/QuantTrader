using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 持仓视图模型
    /// </summary>
    public class PositionViewModel : ViewModelBase
    {
        private string _symbol;
        private int _quantity;
        private decimal _averageCost;
        private decimal _currentPrice;
        private decimal _marketValue;
        private decimal _unrealizedPnL;
        private decimal _unrealizedPnLPercent;

        public string Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public decimal AverageCost
        {
            get => _averageCost;
            set => SetProperty(ref _averageCost, value);
        }

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set => SetProperty(ref _currentPrice, value);
        }

        public decimal MarketValue
        {
            get => _marketValue;
            set => SetProperty(ref _marketValue, value);
        }

        public decimal UnrealizedPnL
        {
            get => _unrealizedPnL;
            set => SetProperty(ref _unrealizedPnL, value);
        }

        public decimal UnrealizedPnLPercent
        {
            get => _unrealizedPnLPercent;
            set => SetProperty(ref _unrealizedPnLPercent, value);
        }
    }
}
