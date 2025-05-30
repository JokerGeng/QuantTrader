using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.Utils;

namespace QuantTrader.Strategies
{
    public class BollingerBandsStrategy : StrategyBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, Level1Data> _latestPrices = new Dictionary<string, Level1Data>();

        public BollingerBandsStrategy(
            string id,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
            : base(id, brokerService, marketDataService, dataRepository)
        {
            Name = "Bollinger Bands Strategy";
            Description = "Buy when price touches the lower band, sell when price touches the upper band";

            // 设置默认参数
            Parameters = new Dictionary<string, object>
            {
                { "Symbol", "AAPL" },
                { "Period", 20 },
                { "Multiplier", 2.0m },
                { "Quantity", 100 },
                { "CandlestickPeriod", TimeSpan.FromMinutes(5) },
                { "MaxPositionValue", 100000m },
                { "ExitMiddleBand", true } // 是否在价格回归到中轨时平仓
            };
        }

        public override async Task StartAsync()
        {
            await base.StartAsync();

            // 取消之前的令牌
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            // 获取参数
            var symbol = Parameters["Symbol"] as string;
            var period = Convert.ToInt32(Parameters["Period"]);
            var candlePeriod = (TimeSpan)Parameters["CandlestickPeriod"];

            // 获取初始K线数据
            await RefreshCandlesticksAsync(symbol, Math.Max(period + 10, 50), candlePeriod);

            // 订阅行情数据
            _marketDataService.SubscribeLevel1Data(symbol, OnLevel1DataReceived);

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
            var symbol = Parameters["Symbol"] as string;

            while (!cancellationToken.IsCancellationRequested && Status == StrategyStatus.Running)
            {
                try
                {
                    // 检查是否需要更新K线数据
                    var period = Convert.ToInt32(Parameters["Period"]);
                    await RefreshCandlesticksAsync(symbol, Math.Max(period + 10, 50), (TimeSpan)Parameters["CandlestickPeriod"]);

                    // 生成交易信号
                    await GenerateSignalsAsync(symbol);

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
            var period = Convert.ToInt32(Parameters["Period"]);
            var multiplier = Convert.ToDecimal(Parameters["Multiplier"]);
            var quantity = Convert.ToInt32(Parameters["Quantity"]);
            var maxPositionValue = Convert.ToDecimal(Parameters["MaxPositionValue"]);
            var exitMiddleBand = Convert.ToBoolean(Parameters["ExitMiddleBand"]);

            // 确保有足够的数据
            if (candles.Count <= period)
            {
                Log($"Not enough data for {symbol}: {candles.Count} candles, need at least {period + 1}");
                return;
            }

            // 计算布林带
            var closePrices = candles.Select(c => c.Close).ToArray();
            var (middle, upper, lower) = IndicatorCalculator.BollingerBands(closePrices, period, multiplier);

            // 检查当前是否有持仓
            bool hasPosition = Positions.TryGetValue(symbol, out var position) && position.Quantity != 0;

            // 获取最新K线
            var lastIndex = candles.Count - 1;
            var lastCandle = candles[lastIndex];
            var lastPrice = lastCandle.Close;

            // 获取当前布林带值
            var currMiddle = middle[lastIndex];
            var currUpper = upper[lastIndex];
            var currLower = lower[lastIndex];

            // 获取前一个布林带值
            var prevMiddle = middle[lastIndex - 1];
            var prevUpper = upper[lastIndex - 1];
            var prevLower = lower[lastIndex - 1];

            // 获取前一个收盘价
            var prevClose = candles[lastIndex - 1].Close;

            // 检查触及下轨买入信号
            bool touchLowerBand = prevClose > prevLower && lastPrice <= currLower;
            // 检查触及上轨卖出信号
            bool touchUpperBand = prevClose < prevUpper && lastPrice >= currUpper;
            // 检查回归中轨信号
            bool touchMiddleBand = (position?.Quantity > 0 && prevClose < prevMiddle && lastPrice >= currMiddle) ||
                                  (position?.Quantity < 0 && prevClose > prevMiddle && lastPrice <= currMiddle);

            // 判断是否生成信号
            if (touchLowerBand && (!hasPosition || position.Quantity < 0))
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
                    Reason = $"Price ({lastPrice:F2}) touched lower band ({currLower:F2})"
                };

                GenerateSignal(signal);

                // 下单
                if (Status == StrategyStatus.Running)
                {
                    await PlaceOrderAsync(signal);
                }
            }
            else if (touchUpperBand && (!hasPosition || position.Quantity > 0))
            {
                // 计算卖出数量
                int sellQuantity = quantity;

                // 如果有多仓，最多卖出全部
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
                    Reason = $"Price ({lastPrice:F2}) touched upper band ({currUpper:F2})"
                };

                GenerateSignal(signal);

                // 下单
                if (Status == StrategyStatus.Running)
                {
                    await PlaceOrderAsync(signal);
                }
            }
            else if (exitMiddleBand && touchMiddleBand && hasPosition && position.Quantity != 0)
            {
                // 计算平仓数量
                int closeQuantity = Math.Abs(position.Quantity);

                // 生成平仓信号
                var signal = new Signal
                {
                    Symbol = symbol,
                    Type = position.Quantity > 0 ? SignalType.Sell : SignalType.Buy,
                    Price = lastPrice,
                    Quantity = closeQuantity,
                    Timestamp = DateTime.Now,
                    Reason = $"Price ({lastPrice:F2}) reverted to middle band ({currMiddle:F2})"
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
