using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 信号视图模型
    /// </summary>
    public class SignalViewModel : ViewModelBase
    {
        private string _strategyId;
        private string _symbol;
        private string _type;
        private decimal _price;
        private int _quantity;
        private DateTime _timestamp;
        private string _reason;

        public string StrategyId
        {
            get => _strategyId;
            set => SetProperty(ref _strategyId, value);
        }

        public string Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }
    }
}
