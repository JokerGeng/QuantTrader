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
    /// <summary>
    /// RSI超买超卖策略
    /// </summary>
    public class RSIStrategy : StrategyBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, Level1Data> _latestPrices = new Dictionary<string, Level1Data>();

        public RSIStrategy(
            string id,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
            : base(id, brokerService, marketDataService, dataRepository)
        {
            Name = "RSI Strategy";
            Description = "Buy when RSI is below oversold level, sell when RSI is above overbought level";

            // 设置默认参数
            Parameters = new Dictionary<string, object>
            {
                { "Symbol", "AAPL" },
                { "RSIPeriod", 14 },
                { "OversoldLevel", 30 },
                { "OverboughtLevel", 70 },
                { "Quantity", 100 },
                { "CandlestickPeriod", TimeSpan.FromMinutes(5) },
                { "MaxPositionValue", 100000m }
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
            var rsiPeriod = Convert.ToInt32(Parameters["RSIPeriod"]);
            var period = (TimeSpan)Parameters["CandlestickPeriod"];

            // 获取初始K线数据
            await RefreshCandlesticksAsync(symbol, Math.Max(rsiPeriod * 3, 50), period);

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
                    var rsiPeriod = Convert.ToInt32(Parameters["RSIPeriod"]);
                    await RefreshCandlesticksAsync(symbol, Math.Max(rsiPeriod * 3, 50), (TimeSpan)Parameters["CandlestickPeriod"]);

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
            var rsiPeriod = Convert.ToInt32(Parameters["RSIPeriod"]);
            var oversoldLevel = Convert.ToInt32(Parameters["OversoldLevel"]);
            var overboughtLevel = Convert.ToInt32(Parameters["OverboughtLevel"]);
            var quantity = Convert.ToInt32(Parameters["Quantity"]);
            var maxPositionValue = Convert.ToDecimal(Parameters["MaxPositionValue"]);

            // 确保有足够的数据
            if (candles.Count < rsiPeriod * 2)
            {
                Log($"Not enough data for {symbol}: {candles.Count} candles, need at least {rsiPeriod * 2}");
                return;
            }

            // 计算RSI
            var closePrices = candles.Select(c => c.Close).ToArray();
            var rsi = IndicatorCalculator.RSI(closePrices, rsiPeriod);

            // 检查当前是否有持仓
            bool hasPosition = Positions.TryGetValue(symbol, out var position) && position.Quantity != 0;

            // 获取最新K线
            var lastIndex = candles.Count - 1;
            var lastCandle = candles[lastIndex];
            var lastPrice = lastCandle.Close;

            // 获取当前RSI值
            var currRSI = rsi[lastIndex];
            var prevRSI = rsi[lastIndex - 1];

            // 检查超买超卖信号
            bool oversold = prevRSI < oversoldLevel && currRSI >= oversoldLevel; // RSI从超卖区域上穿
            bool overbought = prevRSI > overboughtLevel && currRSI <= overboughtLevel; // RSI从超买区域下穿

            if (oversold && (!hasPosition || position.Quantity < 0))
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
                    Reason = $"RSI ({currRSI:F2}) crossed above oversold level ({oversoldLevel})"
                };

                GenerateSignal(signal);

                // 下单
                if (Status == StrategyStatus.Running)
                {
                    await PlaceOrderAsync(signal);
                }
            }
            else if (overbought && (!hasPosition || position.Quantity > 0))
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
                    Reason = $"RSI ({currRSI:F2}) crossed below overbought level ({overboughtLevel})"
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
