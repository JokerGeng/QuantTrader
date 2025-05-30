using System.Collections.ObjectModel;
using System.Windows.Input;
using QuantTrader.Commands;
using QuantTrader.Models;
using QuantTrader.Strategies;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 策略配置视图模型
    /// </summary>
    public class StrategyConfigViewModel : ViewModelBase
    {
        private StrategyInfoBase _strategy;

        public StrategyInfoBase Strategy
        {
            get => _strategy;
            set => SetProperty(ref _strategy, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action SaveRequested;
        public event Action CancelRequested;

        public StrategyConfigViewModel()
        {
            // 初始化命令
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CancelRequested?.Invoke());
        }

        private void ExecuteSave()
        {
            // 触发保存事件
            SaveRequested?.Invoke();
        }
    }
}
