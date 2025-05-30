using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.Utils;

namespace QuantTrader.Strategies
{
    public class MACDStrategy : StrategyBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, Level1Data> _latestPrices = new Dictionary<string, Level1Data>();

        public MACDStrategy(
            string id,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
            : base(id, brokerService, marketDataService, dataRepository)
        {
            Name = "MACD Strategy";
            Description = "Buy when MACD histogram crosses above zero, sell when it crosses below zero";

            // 设置默认参数
            Parameters = new Dictionary<string, object>
            {
                { "Symbol", "AAPL" },
                { "FastPeriod", 12 },
                { "SlowPeriod", 26 },
                { "SignalPeriod", 9 },
                { "Quantity", 100 },
                { "CandlestickPeriod", TimeSpan.FromMinutes(5) },
                { "MaxPositionValue", 100000m },
                { "UseHistogramSignal", true } // 是否使用柱状图交叉信号，否则使用MACD与信号线交叉
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
            var fastPeriod = Convert.ToInt32(Parameters["FastPeriod"]);
            var slowPeriod = Convert.ToInt32(Parameters["SlowPeriod"]);
            var signalPeriod = Convert.ToInt32(Parameters["SignalPeriod"]);
            var candlePeriod = (TimeSpan)Parameters["CandlestickPeriod"];

            // 获取初始K线数据
            int requiredBars = Math.Max(slowPeriod + signalPeriod + 10, 50);
            await RefreshCandlesticksAsync(symbol, requiredBars, candlePeriod);

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
                    var slowPeriod = Convert.ToInt32(Parameters["SlowPeriod"]);
                    var signalPeriod = Convert.ToInt32(Parameters["SignalPeriod"]);
                    int requiredBars = Math.Max(slowPeriod + signalPeriod + 10, 50);

                    await RefreshCandlesticksAsync(symbol, requiredBars, (TimeSpan)Parameters["CandlestickPeriod"]);

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
            var fastPeriod = Convert.ToInt32(Parameters["FastPeriod"]);
            var slowPeriod = Convert.ToInt32(Parameters["SlowPeriod"]);
            var signalPeriod = Convert.ToInt32(Parameters["SignalPeriod"]);
            var quantity = Convert.ToInt32(Parameters["Quantity"]);
            var maxPositionValue = Convert.ToDecimal(Parameters["MaxPositionValue"]);
            var useHistogramSignal = Convert.ToBoolean(Parameters["UseHistogramSignal"]);

            // 确保有足够的数据
            if (candles.Count <= slowPeriod + signalPeriod)
            {
                Log($"Not enough data for {symbol}: {candles.Count} candles, need at least {slowPeriod + signalPeriod + 1}");
                return;
            }

            // 计算MACD
            var closePrices = candles.Select(c => c.Close).ToArray();
            var (macdLine, signalLine, histogram) = IndicatorCalculator.MACD(closePrices, fastPeriod, slowPeriod, signalPeriod);

            // 检查当前是否有持仓
            bool hasPosition = Positions.TryGetValue(symbol, out var position) && position.Quantity != 0;

            // 获取最新K线
            var lastIndex = candles.Count - 1;
            var lastCandle = candles[lastIndex];
            var lastPrice = lastCandle.Close;

            // 获取当前MACD值
            var currMACD = macdLine[lastIndex];
            var currSignal = signalLine[lastIndex];
            var currHistogram = histogram[lastIndex];

            // 获取前一个MACD值
            var prevMACD = macdLine[lastIndex - 1];
            var prevSignal = signalLine[lastIndex - 1];
            var prevHistogram = histogram[lastIndex - 1];

            // 判断交易信号
            bool buySignal = false;
            bool sellSignal = false;
            string signalReason = "";

            if (useHistogramSignal)
            {
                // 使用柱状图穿越零轴作为信号
                buySignal = prevHistogram <= 0 && currHistogram > 0;
                sellSignal = prevHistogram >= 0 && currHistogram < 0;

                signalReason = buySignal
                    ? $"MACD Histogram crossed above zero: {currHistogram:F5}"
                    : $"MACD Histogram crossed below zero: {currHistogram:F5}";
            }
            else
            {
                // 使用MACD线与信号线交叉作为信号
                buySignal = prevMACD <= prevSignal && currMACD > currSignal;
                sellSignal = prevMACD >= prevSignal && currMACD < currSignal;

                signalReason = buySignal
                    ? $"MACD ({currMACD:F5}) crossed above Signal ({currSignal:F5})"
                    : $"MACD ({currMACD:F5}) crossed below Signal ({currSignal:F5})";
            }

            // 判断是否生成信号
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
                    Reason = signalReason
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
                    Reason = signalReason
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
