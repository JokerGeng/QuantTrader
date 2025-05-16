using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 模拟行情数据服务实现
    /// </summary>
    public class SimulatedMarketDataService : IMarketDataService, IDisposable
    {
        private readonly Dictionary<string, Level1Data> _level1Cache = new Dictionary<string, Level1Data>();
        private readonly Dictionary<string, List<Candlestick>> _candlesticksCache = new Dictionary<string, List<Candlestick>>();
        private readonly Dictionary<string, List<Action<Level1Data>>> _level1Subscribers = new Dictionary<string, List<Action<Level1Data>>>();
        private readonly System.Timers.Timer _simulationTimer;
        private readonly Random _random = new Random();

        public SimulatedMarketDataService()
        {
            _simulationTimer = new System.Timers.Timer(1000); // 每秒更新一次模拟数据
            _simulationTimer.Elapsed += OnSimulationTimerElapsed;
            _simulationTimer.Start();

            // 初始化一些模拟数据
            InitializeSimulatedData();
        }

        private void InitializeSimulatedData()
        {
            var symbols = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "FB" };
            var now = DateTime.Now;

            foreach (var symbol in symbols)
            {
                // 创建初始行情数据
                var basePrice = symbol switch
                {
                    "AAPL" => 150.0m,
                    "MSFT" => 300.0m,
                    "GOOGL" => 2800.0m,
                    "AMZN" => 3500.0m,
                    "FB" => 330.0m,
                    _ => 100.0m
                };

                var initialPrice = basePrice * (1 + (decimal)(_random.NextDouble() * 0.02 - 0.01));

                _level1Cache[symbol] = new Level1Data
                {
                    Symbol = symbol,
                    Timestamp = now,
                    LastPrice = initialPrice,
                    Open = initialPrice * 0.99m,
                    High = initialPrice * 1.01m,
                    Low = initialPrice * 0.98m,
                    Volume = _random.Next(1000, 10000),
                    Turnover = initialPrice * _random.Next(1000, 10000),
                    BidPrice1 = initialPrice * 0.999m,
                    BidVolume1 = _random.Next(100, 1000),
                    AskPrice1 = initialPrice * 1.001m,
                    AskVolume1 = _random.Next(100, 1000),
                    PreClose = initialPrice * 0.995m
                };

                // 创建初始K线数据
                var candles = new List<Candlestick>();
                var startTime = now.Date.AddDays(-100);
                var currentPrice = basePrice * 0.8m; // 起始价格是当前价格的80%

                for (int i = 0; i < 100; i++)
                {
                    var time = startTime.AddDays(i);
                    var changePercent = (decimal)(_random.NextDouble() * 0.04 - 0.02); // -2% to +2%
                    var open = currentPrice;
                    var close = open * (1 + changePercent);
                    var high = Math.Max(open, close) * (1 + (decimal)(_random.NextDouble() * 0.01));
                    var low = Math.Min(open, close) * (1 - (decimal)(_random.NextDouble() * 0.01));
                    var volume = _random.Next(100000, 1000000);

                    candles.Add(new Candlestick
                    {
                        Symbol = symbol,
                        Timestamp = time,
                        Period = TimeSpan.FromDays(1),
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = volume
                    });

                    currentPrice = close; // 下一天的价格基于今天的收盘价
                }

                _candlesticksCache[symbol] = candles;
            }
        }

        private void OnSimulationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (var symbol in _level1Cache.Keys.ToList())
            {
                UpdateSimulatedLevel1Data(symbol);
            }
        }

        private void UpdateSimulatedLevel1Data(string symbol)
        {
            if (!_level1Cache.ContainsKey(symbol))
                return;

            var data = _level1Cache[symbol];
            var changePercent = (decimal)(_random.NextDouble() * 0.002 - 0.001); // -0.1% to +0.1%
            var newPrice = data.LastPrice * (1 + changePercent);

            // 更新行情数据
            data.Timestamp = DateTime.Now;
            data.LastPrice = newPrice;
            data.High = Math.Max(data.High, newPrice);
            data.Low = Math.Min(data.Low, newPrice);
            data.Volume += _random.Next(10, 100);
            data.Turnover += newPrice * _random.Next(10, 100);
            data.BidPrice1 = newPrice * 0.999m;
            data.BidVolume1 = _random.Next(100, 1000);
            data.AskPrice1 = newPrice * 1.001m;
            data.AskVolume1 = _random.Next(100, 1000);

            // 通知订阅者
            if (_level1Subscribers.TryGetValue(symbol, out var subscribers))
            {
                foreach (var subscriber in subscribers.ToList())
                {
                    try
                    {
                        subscriber(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error notifying subscriber: {ex.Message}");
                    }
                }
            }

            // 更新K线数据
            if (_candlesticksCache.TryGetValue(symbol, out var candles))
            {
                var lastCandle = candles.LastOrDefault();
                if (lastCandle != null)
                {
                    var now = DateTime.Now;

                    // 如果当前时间超过了最后一根K线的时间加周期，则创建新的K线
                    if (now > lastCandle.Timestamp.Add(lastCandle.Period))
                    {
                        var newCandle = new Candlestick
                        {
                            Symbol = symbol,
                            Timestamp = now.Date, // 简化处理，只按天为周期
                            Period = TimeSpan.FromDays(1),
                            Open = lastCandle.Close,
                            High = newPrice,
                            Low = newPrice,
                            Close = newPrice,
                            Volume = _random.Next(100, 1000)
                        };

                        candles.Add(newCandle);

                        // 保持缓存大小
                        if (candles.Count > 100)
                            candles.RemoveAt(0);
                    }
                    else // 更新当前K线
                    {
                        lastCandle.Close = newPrice;
                        lastCandle.High = Math.Max(lastCandle.High, newPrice);
                        lastCandle.Low = Math.Min(lastCandle.Low, newPrice);
                        lastCandle.Volume += _random.Next(100, 1000);
                    }
                }
            }
        }

        public Task<Level1Data> GetLevel1DataAsync(string symbol)
        {
            if (!_level1Cache.ContainsKey(symbol))
            {
                // 初始化新的行情数据
                var basePrice = 100.0m;
                _level1Cache[symbol] = new Level1Data
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Now,
                    LastPrice = basePrice,
                    Open = basePrice,
                    High = basePrice,
                    Low = basePrice,
                    Volume = 0,
                    Turnover = 0,
                    BidPrice1 = basePrice * 0.999m,
                    BidVolume1 = 100,
                    AskPrice1 = basePrice * 1.001m,
                    AskVolume1 = 100,
                    PreClose = basePrice
                };
            }

            return Task.FromResult(_level1Cache[symbol]);
        }

        public void SubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            if (!_level1Subscribers.ContainsKey(symbol))
            {
                _level1Subscribers[symbol] = new List<Action<Level1Data>>();
            }

            if (!_level1Subscribers[symbol].Contains(callback))
            {
                _level1Subscribers[symbol].Add(callback);
            }

            // 确保有此商品的模拟数据
            GetLevel1DataAsync(symbol).Wait();
        }

        public void UnsubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            if (_level1Subscribers.ContainsKey(symbol))
            {
                _level1Subscribers[symbol].Remove(callback);

                if (_level1Subscribers[symbol].Count == 0)
                {
                    _level1Subscribers.Remove(symbol);
                }
            }
        }

        public Task<List<Candlestick>> GetHistoricalCandlesticksAsync(string symbol, DateTime startTime, DateTime endTime, TimeSpan period)
        {
            if (!_candlesticksCache.ContainsKey(symbol))
            {
                // 初始化新的K线数据
                InitializeSimulatedCandlesticksForSymbol(symbol);
            }

            var candles = _candlesticksCache[symbol]
                .Where(c => c.Timestamp >= startTime && c.Timestamp <= endTime && c.Period == period)
                .ToList();

            return Task.FromResult(candles);
        }

        public Task<List<Candlestick>> GetLatestCandlesticksAsync(string symbol, int count, TimeSpan period)
        {
            if (!_candlesticksCache.ContainsKey(symbol))
            {
                // 初始化新的K线数据
                InitializeSimulatedCandlesticksForSymbol(symbol);
            }

            var candles = _candlesticksCache[symbol]
                .Where(c => c.Period == period)
                .OrderByDescending(c => c.Timestamp)
                .Take(count)
                .OrderBy(c => c.Timestamp)
                .ToList();

            return Task.FromResult(candles);
        }

        private void InitializeSimulatedCandlesticksForSymbol(string symbol)
        {
            var candles = new List<Candlestick>();
            var now = DateTime.Now;
            var startTime = now.Date.AddDays(-100);
            var basePrice = 100.0m;
            var currentPrice = basePrice;

            for (int i = 0; i < 100; i++)
            {
                var time = startTime.AddDays(i);
                var changePercent = (decimal)(_random.NextDouble() * 0.04 - 0.02); // -2% to +2%
                var open = currentPrice;
                var close = open * (1 + changePercent);
                var high = Math.Max(open, close) * (1 + (decimal)(_random.NextDouble() * 0.01));
                var low = Math.Min(open, close) * (1 - (decimal)(_random.NextDouble() * 0.01));
                var volume = _random.Next(100000, 1000000);

                candles.Add(new Candlestick
                {
                    Symbol = symbol,
                    Timestamp = time,
                    Period = TimeSpan.FromDays(1),
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume
                });

                currentPrice = close; // 下一天的价格基于今天的收盘价
            }

            _candlesticksCache[symbol] = candles;
        }

        public void Dispose()
        {
            _simulationTimer?.Stop();
            _simulationTimer?.Dispose();
        }
    }
}
