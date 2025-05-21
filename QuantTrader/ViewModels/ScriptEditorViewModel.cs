using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Commands;
using QuantTrader.Strategies;
using QuantTrader.TradingEngine;
using System.Windows.Input;

namespace QuantTrader.ViewModels
{
    public class ScriptEditorViewModel : ViewModelBase
    {
        private readonly ITradingEngine _tradingEngine;

        private string _scriptCode;
        private string _scriptOutput;
        private string _scriptName;
        private string _selectedTemplate;
        private bool _isSaving;
        private bool _isValidating;
        private bool _isNewScript;
        private ScriptStrategy _scriptStrategy;

        public string ScriptCode
        {
            get => _scriptCode;
            set
            {
                if (SetProperty(ref _scriptCode, value))
                {
                    TemplateChanged?.Invoke();
                }

            }
        }

        public string ScriptOutput
        {
            get => _scriptOutput;
            set => SetProperty(ref _scriptOutput, value);
        }

        public string ScriptName
        {
            get => _scriptName;
            set => SetProperty(ref _scriptName, value);
        }

        public string SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (SetProperty(ref _selectedTemplate, value))
                {
                    LoadTemplate(value);
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public bool IsValidating
        {
            get => _isValidating;
            set => SetProperty(ref _isValidating, value);
        }

        public bool IsNewScript
        {
            get => _isNewScript;
            set => SetProperty(ref _isNewScript, value);
        }

        public Dictionary<string, object> Parameters { get; private set; } = new Dictionary<string, object>();
        public List<string> Templates { get; } = new List<string> { "移动平均线交叉", "RSI超买超卖", "布林带策略", "MACD策略", "空白策略" };

        public ICommand ValidateCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<ScriptStrategy> ScriptSaved;
        public event Action EditorCancelled;
        public event Action TemplateChanged;

        public ScriptEditorViewModel(ITradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;

            // 初始化默认属性
            IsNewScript = true;
            SelectedTemplate = "移动平均线交叉"; // 默认选择移动平均线交叉模板

            // 初始化命令
            ValidateCommand = new AsyncRelayCommand(ExecuteValidateAsync);
            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync);
            CancelCommand = new RelayCommand(_ => EditorCancelled?.Invoke());

            // 初始化默认参数
            InitializeDefaultParameters();
        }

        public void InitializeFromStrategy(ScriptStrategy strategy)
        {
            if (strategy == null)
                return;

            _scriptStrategy = strategy;
            IsNewScript = false;
            ScriptName = strategy.Name;
            ScriptCode = strategy.ScriptCode;

            // 复制参数
            Parameters.Clear();
            foreach (var param in strategy.Parameters)
            {
                Parameters[param.Key] = param.Value;
            }
        }

        private void InitializeDefaultParameters()
        {
            Parameters["Symbol"] = "AAPL";
            Parameters["CandlestickPeriod"] = TimeSpan.FromMinutes(5);
            Parameters["LookbackPeriod"] = 50;
            Parameters["Quantity"] = 100;
            Parameters["MaxPositionValue"] = 100000m;
        }

        private void LoadTemplate(string templateName)
        {
            switch (templateName)
            {
                case "移动平均线交叉":
                    ScriptCode = GetMovingAverageTemplate();
                    break;
                case "RSI超买超卖":
                    ScriptCode = GetRSITemplate();
                    break;
                case "布林带策略":
                    ScriptCode = GetBollingerBandsTemplate();
                    break;
                case "MACD策略":
                    ScriptCode = GetMACDTemplate();
                    break;
                case "空白策略":
                    ScriptCode = GetEmptyTemplate();
                    break;
                default:
                    ScriptCode = GetEmptyTemplate();
                    break;
            }
        }

        private async Task ExecuteValidateAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ScriptCode))
            {
                ScriptOutput = "Error: Script code is empty";
                return;
            }

            try
            {
                IsValidating = true;
                ScriptOutput = "Validating script...";

                // 创建临时策略用于验证
                var tempStrategy = new ScriptStrategy(
                    "Temp_" + Guid.NewGuid().ToString("N"),
                    null,  // 这些依赖项在验证时不需要
                    null,
                    null);

                tempStrategy.ScriptCode = ScriptCode;

                // 尝试编译脚本
                await Task.Run(() => tempStrategy.InitializeAsync());

                ScriptOutput = "Script validation successful";
            }
            catch (Exception ex)
            {
                ScriptOutput = $"Validation error: {ex.Message}";
            }
            finally
            {
                IsValidating = false;
            }
        }

        private async Task ExecuteSaveAsync(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ScriptName))
            {
                ScriptOutput = "Error: Strategy name is required";
                return;
            }

            if (string.IsNullOrWhiteSpace(ScriptCode))
            {
                ScriptOutput = "Error: Script code is empty";
                return;
            }

            try
            {
                IsSaving = true;
                ScriptOutput = "Saving script...";

                // 如果是编辑现有策略
                if (!IsNewScript && _scriptStrategy != null)
                {
                    _scriptStrategy.Name = ScriptName;
                    _scriptStrategy.ScriptCode = ScriptCode;

                    // 更新参数
                    await _tradingEngine.UpdateStrategyParametersAsync(_scriptStrategy.Id, Parameters);

                    ScriptSaved?.Invoke(_scriptStrategy);
                }
                else
                {
                    // 创建新的自定义策略
                    var strategyId = $"Script_{Guid.NewGuid():N}";

                    // 添加策略
                    var strategy = await _tradingEngine.AddStrategyAsync("script", Parameters) as ScriptStrategy;

                    if (strategy != null)
                    {
                        strategy.Name = ScriptName;
                        strategy.ScriptCode = ScriptCode;

                        ScriptSaved?.Invoke(strategy);
                    }
                    else
                    {
                        throw new Exception("Failed to create script strategy");
                    }
                }
            }
            catch (Exception ex)
            {
                ScriptOutput = $"Save error: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        private string GetMovingAverageTemplate()
        {
            return @"
// 移动平均线交叉策略
// 此策略在快速移动平均线上穿慢速移动平均线时买入，
// 在快速移动平均线下穿慢速移动平均线时卖出。

// 提取收盘价
var closePrices = Candles.Select(c => c.Close).ToArray();

// 计算两条移动平均线
int fastPeriod = 5;
int slowPeriod = 20;
var fastMA = IndicatorCalculator.SMA(closePrices, fastPeriod);
var slowMA = IndicatorCalculator.SMA(closePrices, slowPeriod);

// 获取最新值
int lastIndex = Candles.Count - 1;
if (lastIndex < slowPeriod)
{
    Logger(""Not enough data"");
    return null;
}

var currFastMA = fastMA[lastIndex];
var currSlowMA = slowMA[lastIndex];
var prevFastMA = fastMA[lastIndex - 1];
var prevSlowMA = slowMA[lastIndex - 1];

// 检查交叉信号
bool buySignal = prevFastMA <= prevSlowMA && currFastMA > currSlowMA;
bool sellSignal = prevFastMA >= prevSlowMA && currFastMA < currSlowMA;

// 生成信号
if (buySignal && (Position == null || Position.Quantity <= 0))
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""Fast MA ({currFastMA:F2}) crossed above Slow MA ({currSlowMA:F2})""
    };
}
else if (sellSignal && Position != null && Position.Quantity > 0)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = Math.Min(100, Position.Quantity),
        Reason = $""Fast MA ({currFastMA:F2}) crossed below Slow MA ({currSlowMA:F2})""
    };
}

// 返回结果
return CurrentSignal;
";
        }

        private string GetRSITemplate()
        {
            return @"
// RSI超买超卖策略
// 此策略在RSI超卖后回升时买入，RSI超买后回落时卖出

// 提取收盘价
var closePrices = Candles.Select(c => c.Close).ToArray();

// 计算RSI指标
int rsiPeriod = 14;
int oversoldLevel = 30;
int overboughtLevel = 70;
var rsi = IndicatorCalculator.RSI(closePrices, rsiPeriod);

// 获取最新值
int lastIndex = Candles.Count - 1;
if (lastIndex < rsiPeriod * 2)
{
    Logger(""Not enough data"");
    return null;
}

var currRSI = rsi[lastIndex];
var prevRSI = rsi[lastIndex - 1];

// 检查超买超卖信号
bool oversold = prevRSI < oversoldLevel && currRSI >= oversoldLevel; // RSI从超卖区域上穿
bool overbought = prevRSI > overboughtLevel && currRSI <= overboughtLevel; // RSI从超买区域下穿

// 生成信号
if (oversold && (Position == null || Position.Quantity <= 0))
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""RSI ({currRSI:F2}) crossed above oversold level ({oversoldLevel})""
    };
}
else if (overbought && Position != null && Position.Quantity > 0)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = Math.Min(100, Position.Quantity),
        Reason = $""RSI ({currRSI:F2}) crossed below overbought level ({overboughtLevel})""
    };
}

// 返回结果
return CurrentSignal;
";
        }

        private string GetBollingerBandsTemplate()
        {
            return @"
// 布林带策略
// 此策略在价格触及下轨时买入，触及上轨时卖出

// 提取收盘价
var closePrices = Candles.Select(c => c.Close).ToArray();

// 计算布林带
int period = 20;
decimal multiplier = 2.0m;
var (middle, upper, lower) = IndicatorCalculator.BollingerBands(closePrices, period, multiplier);

// 获取最新值
int lastIndex = Candles.Count - 1;
if (lastIndex < period)
{
    Logger(""Not enough data"");
    return null;
}

var lastPrice = Candles[lastIndex].Close;
var prevPrice = Candles[lastIndex - 1].Close;

var currMiddle = middle[lastIndex];
var currUpper = upper[lastIndex];
var currLower = lower[lastIndex];

var prevMiddle = middle[lastIndex - 1];
var prevUpper = upper[lastIndex - 1];
var prevLower = lower[lastIndex - 1];

// 检查触及信号
bool touchLowerBand = prevPrice > prevLower && lastPrice <= currLower;
bool touchUpperBand = prevPrice < prevUpper && lastPrice >= currUpper;

// 生成信号
if (touchLowerBand && (Position == null || Position.Quantity <= 0))
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""Price ({lastPrice:F2}) touched lower band ({currLower:F2})""
    };
}
else if (touchUpperBand && Position != null && Position.Quantity > 0)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = Math.Min(100, Position.Quantity),
        Reason = $""Price ({lastPrice:F2}) touched upper band ({currUpper:F2})""
    };
}

// 返回结果
return CurrentSignal;
";
        }

        private string GetMACDTemplate()
        {
            return @"
// MACD策略
// 此策略在MACD柱状图由负转正时买入，由正转负时卖出

// 提取收盘价
var closePrices = Candles.Select(c => c.Close).ToArray();

// 计算MACD
int fastPeriod = 12;
int slowPeriod = 26;
int signalPeriod = 9;
var (macdLine, signalLine, histogram) = IndicatorCalculator.MACD(closePrices, fastPeriod, slowPeriod, signalPeriod);

// 获取最新值
int lastIndex = Candles.Count - 1;
if (lastIndex < slowPeriod + signalPeriod)
{
    Logger(""Not enough data"");
    return null;
}

var currHistogram = histogram[lastIndex];
var prevHistogram = histogram[lastIndex - 1];

// 检查MACD柱状图交叉信号
bool buySignal = prevHistogram <= 0 && currHistogram > 0;  // 柱状图由负转正
bool sellSignal = prevHistogram >= 0 && currHistogram < 0; // 柱状图由正转负

// 生成信号
if (buySignal && (Position == null || Position.Quantity <= 0))
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""MACD Histogram crossed above zero: {currHistogram:F5}""
    };
}
else if (sellSignal && Position != null && Position.Quantity > 0)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = Math.Min(100, Position.Quantity),
        Reason = $""MACD Histogram crossed below zero: {currHistogram:F5}""
    };
}

// 返回结果
return CurrentSignal;
";
        }

        private string GetEmptyTemplate()
        {
            return @"
// 自定义策略模板
// 请在这里编写您的策略逻辑

// 可用变量:
// - Candles: K线数据列表
// - Symbol: 交易品种
// - Parameters: 策略参数
// - Position: 当前持仓
// - Logger: 日志记录函数

// 返回值:
// - 设置 CurrentSignal 生成交易信号
// - 返回 null 表示不生成信号

// 提取收盘价
var closePrices = Candles.Select(c => c.Close).ToArray();

// 获取最新价格
int lastIndex = Candles.Count - 1;
if (lastIndex < 0)
{
    Logger(""No data available"");
    return null;
}

var lastPrice = closePrices[lastIndex];

// TODO: 在这里编写您的策略逻辑

// 示例: 当价格高于某个阈值时卖出，低于某个阈值时买入
decimal buyThreshold = 150m;
decimal sellThreshold = 200m;

if (lastPrice < buyThreshold && (Position == null || Position.Quantity <= 0))
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""Price ({lastPrice:F2}) below buy threshold ({buyThreshold:F2})""
    };
}
else if (lastPrice > sellThreshold && Position != null && Position.Quantity > 0)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = Math.Min(100, Position.Quantity),
        Reason = $""Price ({lastPrice:F2}) above sell threshold ({sellThreshold:F2})""
    };
}

// 返回结果
return CurrentSignal;
";
        }
    }
}
