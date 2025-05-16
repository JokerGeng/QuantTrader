using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    public class AccountViewModel : ViewModelBase
    {
        private string _accountId;
        private decimal _cash;
        private decimal _totalAssetValue;
        private decimal _marginUsed;
        private decimal _marginAvailable;

        public string AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }

        public decimal Cash
        {
            get => _cash;
            set => SetProperty(ref _cash, value);
        }

        public decimal TotalAssetValue
        {
            get => _totalAssetValue;
            set => SetProperty(ref _totalAssetValue, value);
        }

        public decimal MarginUsed
        {
            get => _marginUsed;
            set => SetProperty(ref _marginUsed, value);
        }

        public decimal MarginAvailable
        {
            get => _marginAvailable;
            set => SetProperty(ref _marginAvailable, value);
        }

        public decimal PnL => TotalAssetValue - 1000000; // 假设初始资金为100万
        public decimal PnLPercent => 1000000 == 0 ? 0 : decimal.Round(PnL / 1000000 * 100, 2);
    }
}
