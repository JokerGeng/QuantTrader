using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    public class OrderViewModel : ViewModelBase
    {
        private string _orderId;
        private string _strategyId;
        private string _symbol;
        private string _direction;
        private string _type;
        private decimal _price;
        private int _quantity;
        private int _filledQuantity;
        private string _status;
        private DateTime _createTime;
        private DateTime _updateTime;
        private decimal _averageFilledPrice;

        public string OrderId
        {
            get => _orderId;
            set => SetProperty(ref _orderId, value);
        }

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

        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
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

        public int FilledQuantity
        {
            get => _filledQuantity;
            set => SetProperty(ref _filledQuantity, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        public DateTime UpdateTime
        {
            get => _updateTime;
            set => SetProperty(ref _updateTime, value);
        }

        public decimal AverageFilledPrice
        {
            get => _averageFilledPrice;
            set => SetProperty(ref _averageFilledPrice, value);
        }

        public decimal FilledValue => FilledQuantity * AverageFilledPrice;
        public string FillPercentage => $"{(Quantity == 0 ? 0 : (decimal)FilledQuantity / Quantity * 100):F2}%";
    }
}
