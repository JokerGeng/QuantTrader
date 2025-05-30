using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using QuantTrader.BrokerServices;
using QuantTrader.Models;
using QuantTrader.Utils;
using QuantTrader.MarketDatas;

namespace QuantTrader.Strategies
{
    public class ScriptStrategy : StrategyBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, Level1Data> _latestPrices = new Dictionary<string, Level1Data>();

        private string _scriptCode;
        //private ScriptRunner<object> _compiledScript;
        private Script<object> _compiledScript;
        private bool _isScriptCompiled;

        public string ScriptCode
        {
            get => _scriptCode;
            set
            {
                _scriptCode = value;
                _isScriptCompiled = false;
            }
        }

        public ScriptStrategy(IStrategyInfo strategyInfo,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
            : base(strategyInfo, brokerService, marketDataService, dataRepository)
        {
            ScriptCode = GetDefaultScript();
        }

        public override async Task InitializeAsync()
        {
            //await base.InitializeAsync();

            // 尝试编译脚本
            if (!_isScriptCompiled)
            {
                await CompileScriptAsync();
            }
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            // 确保脚本已编译
            if (!_isScriptCompiled)
            {
                await CompileScriptAsync();
            }

            // 取消之前的令牌
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // 获取参数
            var lookbackPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "LookbackPeriod").Value);
            var period = (TimeSpan)StrategyInfo.Parameters.Find(t => t.Name == "CandlestickPeriod").Value;

            // 获取初始K线数据
            await RefreshCandlesticksAsync(Symbol, lookbackPeriod, period);

            // 订阅行情数据
            _marketDataService.SubscribeLevel1Data(Symbol, OnLevel1DataReceived);

            // 启动策略循环
            Task.Run(() => RunStrategyLoopAsync(_cancellationTokenSource.Token));
        }

        public override async Task StopAsync()
        {
            // 取消策略循环
            _cancellationTokenSource?.Cancel();

            // 停止行情订阅
            foreach (var symbol in _latestPrices.Keys.ToList())
            {
                _marketDataService.UnsubscribeLevel1Data(symbol, OnLevel1DataReceived);
            }

            await base.StopAsync();
        }

        private async Task CompileScriptAsync()
        {
            try
            {
                // 创建脚本选项
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(typeof(System.Math).Assembly)  // 添加System引用
                    .WithReferences(typeof(IndicatorCalculator).Assembly) // 添加本项目引用
                    .WithImports("System", "System.Collections.Generic", "System.Linq",
                                "QuantTrader.Models", "QuantTrader.Utils");

                // 编译脚本
                _compiledScript = CSharpScript.Create<object>(_scriptCode, scriptOptions,
                    globalsType: typeof(ScriptGlobals));

                 _compiledScript.Compile();

                _isScriptCompiled = true;
                //Log("Script compiled successfully");
            }
            catch (Exception ex)
            {
                _isScriptCompiled = false;
                Log($"Script compilation error: {ex.Message}");
                throw new Exception($"Failed to compile strategy script: {ex.Message}");
            }
        }

        private async Task RunStrategyLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && Status == StrategyStatus.Running)
            {
                try
                {
                    var lookbackPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "LookbackPeriod").Value);
                    var period = (TimeSpan)StrategyInfo.Parameters.Find(t => t.Name == "CandlestickPeriod").Value;
                    // 检查是否需要更新K线数据
                    await RefreshCandlesticksAsync(Symbol, lookbackPeriod, period);

                    // 生成交易信号
                    await GenerateSignalsAsync(Symbol);

                    // 等待下一个周期
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                    break;
                }
                catch (Exception ex)
                {
                    Log($"Error in strategy loop: {ex.Message}");
                    Status = StrategyStatus.Error;
                    break;
                }
            }
        }

        private void OnLevel1DataReceived(Level1Data data)
        {
            if (Status != StrategyStatus.Running)
                return;

            // 更新最新价格
            _latestPrices[data.Symbol] = data;

            // 更新持仓的市场价值
            if (Positions.TryGetValue(data.Symbol, out var position))
            {
                position.UpdatePrice(data.LastPrice);
            }
        }

        private async Task RefreshCandlesticksAsync(string symbol, int count, TimeSpan period)
        {
            // 获取最新K线数据
            var candles = await _marketDataService.GetLatestCandlesticksAsync(symbol, count, period);
            _candlesticksCache[symbol] = candles;
        }

        private async Task GenerateSignalsAsync(string symbol)
        {
            if (!_candlesticksCache.TryGetValue(symbol, out var candles) || candles.Count == 0)
                return;

            if (!_isScriptCompiled)
                return;

            try
            {
                // 准备脚本全局变量
                var globals = new ScriptGlobals
                {
                    Candles = candles,
                    Symbol = symbol,
                    Parameters = StrategyInfo.Parameters,
                    Position = Positions.TryGetValue(symbol, out var position) ? position : new Position(symbol),
                    CurrentSignal = null,
                    Logger = message => Log(message)
                };

                // 执行脚本
                var result = await _compiledScript.RunAsync(globals);

                // 检查脚本生成的信号
                if (globals.CurrentSignal != null)
                {
                    var signal = globals.CurrentSignal;

                    // 确保信号的基本属性设置正确
                    signal.Symbol = symbol;
                    signal.Timestamp = DateTime.Now;

                    // 如果价格为0，使用最新价格
                    if (signal.Price == 0 && candles.Count > 0)
                    {
                        signal.Price = candles.Last().Close;
                    }

                    // 默认数量
                    if (signal.Quantity == 0)
                    {
                        var quantity = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "Quantity").Value);
                        signal.Quantity = quantity;
                    }

                    // 生成信号
                    GenerateSignal(signal);

                    // 执行交易
                    if (Status == StrategyStatus.Running)
                    {
                        await PlaceOrderAsync(signal);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error executing strategy script: {ex.Message}");
            }
        }

        private string GetDefaultScript()
        {
            return @"
// 这是一个简单的移动平均线交叉策略示例
// 您可以修改这个脚本来创建自己的交易策略

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

// 生成信号
if (prevFastMA <= prevSlowMA && currFastMA > currSlowMA)
{
    // 买入信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Buy,
        Quantity = 100,
        Reason = $""Fast MA ({currFastMA:F2}) crossed above Slow MA ({currSlowMA:F2})""
    };
}
else if (prevFastMA >= prevSlowMA && currFastMA < currSlowMA)
{
    // 卖出信号
    CurrentSignal = new Signal
    {
        Type = SignalType.Sell,
        Quantity = 100,
        Reason = $""Fast MA ({currFastMA:F2}) crossed below Slow MA ({currSlowMA:F2})""
    };
}

return CurrentSignal;
";
        }
    }

    /// <summary>
    /// 脚本全局变量，提供给脚本使用的上下文
    /// </summary>
    public class ScriptGlobals
    {
        // 数据
        public List<Candlestick> Candles { get; set; }
        public string Symbol { get; set; }
        public List<StrategyParameter> Parameters { get; set; }
        public Position Position { get; set; }

        // 输出
        public Signal CurrentSignal { get; set; }

        // 工具
        public Action<string> Logger { get; set; }
    }
}
