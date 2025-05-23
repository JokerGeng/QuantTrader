using System.Collections.ObjectModel;
using System.Windows.Input;
using QuantTrader.Commands;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 策略配置视图模型
    /// </summary>
    public class StrategyConfigViewModel : ViewModelBase
    {
        private string _strategyType;
        private bool _isNewStrategy;
        private string _strategyId;
        private string _strategyName;

        public string StrategyType
        {
            get => _strategyType;
            set
            {
                if (SetProperty(ref _strategyType, value))
                {
                    // 根据策略类型加载默认参数
                    LoadDefaultParameters();
                }
            }
        }

        public bool IsNewStrategy
        {
            get => _isNewStrategy;
            set => SetProperty(ref _isNewStrategy, value);
        }

        public string StrategyId
        {
            get => _strategyId;
            set => SetProperty(ref _strategyId, value);
        }

        public string StrategyName
        {
            get => _strategyName;
            set => SetProperty(ref _strategyName, value);
        }

        public ObservableCollection<StrategyParameterViewModel> Parameters { get; }
            = new ObservableCollection<StrategyParameterViewModel>();

        public ObservableCollection<string> AvailableStrategyTypes { get; }
            = new ObservableCollection<string>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<Dictionary<string, object>> SaveRequested;
        public event Action CancelRequested;

        public StrategyConfigViewModel()
        {
            // 初始化可用策略类型
            AvailableStrategyTypes.Add("MovingAverageCross");
            AvailableStrategyTypes.Add("RSI");
            AvailableStrategyTypes.Add("BollingerBands");
            AvailableStrategyTypes.Add("MACD");

            // 默认选择第一个策略类型
            if (AvailableStrategyTypes.Count > 0)
            {
                StrategyType = AvailableStrategyTypes[0];
            }

            // 初始化命令
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(() => CancelRequested?.Invoke());

            // 默认为新策略
            IsNewStrategy = true;
        }

        /// <summary>
        /// 根据策略类型加载默认参数
        /// </summary>
        public void LoadDefaultParameters()
        {
            Parameters.Clear();

            switch (StrategyType)
            {
                case "MovingAverageCross":
                    StrategyName = "Moving Average Cross";
                    Parameters.Add(new StrategyParameterViewModel("Symbol", "AAPL", "Symbol to trade", "string"));
                    Parameters.Add(new StrategyParameterViewModel("FastPeriod", 5, "Fast moving average period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("SlowPeriod", 20, "Slow moving average period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("Quantity", 100, "Quantity to trade", "int"));
                    Parameters.Add(new StrategyParameterViewModel("MaxPositionValue", 100000m, "Maximum position value", "decimal"));
                    break;

                case "RSI":
                    StrategyName = "RSI Strategy";
                    Parameters.Add(new StrategyParameterViewModel("Symbol", "AAPL", "Symbol to trade", "string"));
                    Parameters.Add(new StrategyParameterViewModel("RSIPeriod", 14, "RSI calculation period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("OversoldLevel", 30, "Oversold level", "int"));
                    Parameters.Add(new StrategyParameterViewModel("OverboughtLevel", 70, "Overbought level", "int"));
                    Parameters.Add(new StrategyParameterViewModel("Quantity", 100, "Quantity to trade", "int"));
                    Parameters.Add(new StrategyParameterViewModel("MaxPositionValue", 100000m, "Maximum position value", "decimal"));
                    break;

                case "BollingerBands":
                    StrategyName = "Bollinger Bands Strategy";
                    Parameters.Add(new StrategyParameterViewModel("Symbol", "AAPL", "Symbol to trade", "string"));
                    Parameters.Add(new StrategyParameterViewModel("Period", 20, "Calculation period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("Multiplier", 2.0m, "Standard deviation multiplier", "decimal"));
                    Parameters.Add(new StrategyParameterViewModel("Quantity", 100, "Quantity to trade", "int"));
                    Parameters.Add(new StrategyParameterViewModel("MaxPositionValue", 100000m, "Maximum position value", "decimal"));
                    break;

                case "MACD":
                    StrategyName = "MACD Strategy";
                    Parameters.Add(new StrategyParameterViewModel("Symbol", "AAPL", "Symbol to trade", "string"));
                    Parameters.Add(new StrategyParameterViewModel("FastPeriod", 12, "Fast EMA period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("SlowPeriod", 26, "Slow EMA period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("SignalPeriod", 9, "Signal period", "int"));
                    Parameters.Add(new StrategyParameterViewModel("Quantity", 100, "Quantity to trade", "int"));
                    Parameters.Add(new StrategyParameterViewModel("MaxPositionValue", 100000m, "Maximum position value", "decimal"));
                    break;
            }
        }

        /// <summary>
        /// 设置现有策略的参数
        /// </summary>
        public void SetExistingStrategy(StrategyViewModel strategy)
        {
            IsNewStrategy = false;
            StrategyId = strategy.Id;
            StrategyName = strategy.Name;

            // 根据策略名称推断类型
            if (strategy.Name.Contains("Moving Average"))
                StrategyType = "MovingAverageCross";
            else if (strategy.Name.Contains("RSI"))
                StrategyType = "RSI";
            else if (strategy.Name.Contains("Bollinger"))
                StrategyType = "BollingerBands";
            else if (strategy.Name.Contains("MACD"))
                StrategyType = "MACD";

            // 设置参数值
            foreach (var parameter in Parameters)
            {
                if (strategy.Parameters.TryGetValue(parameter.Name, out var value))
                {
                    parameter.Value = value;
                }
            }
        }

        private void ExecuteSave()
        {
            // 收集参数
            var parameters = new Dictionary<string, object>();

            foreach (var parameter in Parameters)
            {
                parameters[parameter.Name] = parameter.Value;
            }

            // 触发保存事件
            SaveRequested?.Invoke(parameters);
        }
    }
}
