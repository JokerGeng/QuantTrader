using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 日志条目视图模型
    /// </summary>
    public class LogEntryViewModel : ViewModelBase
    {
        private DateTime _timestamp;
        private string _strategyId;
        private string _message;

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public string StrategyId
        {
            get => _strategyId;
            set => SetProperty(ref _strategyId, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }
    }
}
