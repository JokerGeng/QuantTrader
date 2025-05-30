using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.Utils;

namespace QuantTrader.Strategies
{
    public class MovingAverageCrossStrategy : StrategyBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, Level1Data> _latestPrices = new Dictionary<string, Level1Data>();

        public MovingAverageCrossStrategy(IStrategyInfo strategyInfo,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
            : base(strategyInfo, brokerService, marketDataService, dataRepository)
        {
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            // 取消之前的令牌
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // 获取参数
            var fastPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "FastPeriod").Value);
            var slowPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "SlowPeriod").Value);
            var period = (TimeSpan)StrategyInfo.Parameters.Find(t => t.Name == "CandlestickPeriod").Value;

            // 确保慢周期大于快周期
            if (slowPeriod <= fastPeriod)
            {
                Log($"Invalid parameters: SlowPeriod ({slowPeriod}) must be greater than FastPeriod ({fastPeriod})");
                Status = StrategyStatus.Error;
                return;
            }

            // 获取初始K线数据
            await RefreshCandlesticksAsync(Symbol, Math.Max(slowPeriod, 50), period);

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

        private async Task RunStrategyLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && Status == StrategyStatus.Running)
            {
                try
                {
                    var slowPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "SlowPeriod").Value);
                    var period = (TimeSpan)StrategyInfo.Parameters.Find(t => t.Name == "CandlestickPeriod").Value;
                    // 检查是否需要更新K线数据
                    await RefreshCandlesticksAsync(Symbol, Math.Max(slowPeriod, 50), period);

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

            // 获取参数
            var fastPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "FastPeriod").Value);
            var slowPeriod = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "SlowPeriod").Value);
            var quantity = Convert.ToInt32(StrategyInfo.Parameters.Find(t => t.Name == "Quantity").Value);
            var maxPositionValue = Convert.ToDecimal(StrategyInfo.Parameters.Find(t => t.Name == "MaxPositionValue").Value);

            // 确保有足够的数据
            if (candles.Count < slowPeriod)
            {
                Log($"Not enough data for {symbol}: {candles.Count} candles, need at least {slowPeriod}");
                return;
            }

            // 计算移动平均线
            var closePrices = candles.Select(c => c.Close).ToArray();
            var fastMA = IndicatorCalculator.SMA(closePrices, fastPeriod);
            var slowMA = IndicatorCalculator.SMA(closePrices, slowPeriod);

            // 检查当前是否有持仓
            bool hasPosition = Positions.TryGetValue(symbol, out var position) && position.Quantity != 0;

            // 获取最新K线
            var lastIndex = candles.Count - 1;
            var lastCandle = candles[lastIndex];
            var lastPrice = lastCandle.Close;

            // 计算前一个周期的MA值
            var prevFastMA = fastMA[lastIndex - 1];
            var prevSlowMA = slowMA[lastIndex - 1];

            // 计算当前周期的MA值
            var currFastMA = fastMA[lastIndex];
            var currSlowMA = slowMA[lastIndex];

            // 检查交叉信号
            bool buySignal = prevFastMA <= prevSlowMA && currFastMA > currSlowMA;
            bool sellSignal = prevFastMA >= prevSlowMA && currFastMA < currSlowMA;

            if (buySignal && (!hasPosition || position.Quantity < 0))
            {
                // 计算买入数量
                int buyQuantity = quantity;

                // 如果有空仓，先平仓
                if (hasPosition && position.Quantity < 0)
                {
                    buyQuantity += Math.Abs(position.Quantity);
                }

                // 检查是否超过最大持仓限制
                decimal potentialPositionValue = lastPrice * buyQuantity;
                if (potentialPositionValue > maxPositionValue)
                {
                    buyQuantity = (int)(maxPositionValue / lastPrice);
                    if (buyQuantity <= 0)
                    {
                        Log($"Buy signal for {symbol} ignored: insufficient funds for minimum quantity");
                        return;
                    }
                }

                // 生成买入信号
                var signal = new Signal
                {
                    Symbol = symbol,
                    Type = SignalType.Buy,
                    Price = lastPrice,
                    Quantity = buyQuantity,
                    Timestamp = DateTime.Now,
                    Reason = $"Fast MA ({currFastMA:F2}) crossed above Slow MA ({currSlowMA:F2})"
                };

                GenerateSignal(signal);

                // 下单
                if (Status == StrategyStatus.Running)
                {
                    await PlaceOrderAsync(signal);
                }
            }
            else if (sellSignal && (!hasPosition || position.Quantity > 0))
            {
                // 计算卖出数量
                int sellQuantity = quantity;

                // 如果有多仓，先平仓
                if (hasPosition && position.Quantity > 0)
                {
                    sellQuantity = Math.Min(sellQuantity, position.Quantity);
                }

                // 生成卖出信号
                var signal = new Signal
                {
                    Symbol = symbol,
                    Type = SignalType.Sell,
                    Price = lastPrice,
                    Quantity = sellQuantity,
                    Timestamp = DateTime.Now,
                    Reason = $"Fast MA ({currFastMA:F2}) crossed below Slow MA ({currSlowMA:F2})"
                };

                GenerateSignal(signal);

                // 下单
                if (Status == StrategyStatus.Running)
                {
                    await PlaceOrderAsync(signal);
                }
            }
        }
    }
}
