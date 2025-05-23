using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    /// <summary>
    /// 股票信息模型
    /// </summary>
    public class StockInfo : INotifyPropertyChanged
    {
        private decimal _currentPrice;
        private decimal _changePercent;
        private decimal _volume;
        private bool _isSelected;
        private bool _hasStrategy;
        private string _strategyStatus;

        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Market { get; set; }
        public string Industry { get; set; }
        public string Pinyin { get; set; } // 拼音缩写，用于搜索

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set
            {
                _currentPrice = value;
                OnPropertyChanged(nameof(CurrentPrice));
            }
        }

        public decimal ChangePercent
        {
            get => _changePercent;
            set
            {
                _changePercent = value;
                OnPropertyChanged(nameof(ChangePercent));
                OnPropertyChanged(nameof(ChangeColor));
            }
        }

        public decimal Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public bool HasStrategy
        {
            get => _hasStrategy;
            set
            {
                _hasStrategy = value;
                OnPropertyChanged(nameof(HasStrategy));
            }
        }

        public string StrategyStatus
        {
            get => _strategyStatus;
            set
            {
                _strategyStatus = value;
                OnPropertyChanged(nameof(StrategyStatus));
            }
        }

        public string ChangeColor => ChangePercent >= 0 ? "Red" : "Green";
        public string VolumeDisplay => Volume > 100000000 ? $"{Volume / 100000000:F1}亿" : $"{Volume / 10000:F0}万";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
