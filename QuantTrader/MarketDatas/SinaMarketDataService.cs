using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace QuantTrader.MarketDatas
{
    public class SinaMarketDataService : IMarketDataService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, Level1Data> _level1Cache = new Dictionary<string, Level1Data>();
        private readonly Dictionary<string, List<Action<Level1Data>>> _level1Subscribers = new Dictionary<string, List<Action<Level1Data>>>();
        private readonly Dictionary<string, List<Candlestick>> _candleCache = new Dictionary<string, List<Candlestick>>();
        private readonly System.Timers.Timer _updateTimer;
        private readonly object _lockObject = new object();

        // 新浪财经接口URL模板
        private const string SinaQuoteUrl = "http://hq.sinajs.cn/list={0}";
        private const string SinaKLineUrl = "https://finance.sina.com.cn/realstock/company/{0}/jsvar.js"; // 简化的K线接口

        public SinaMarketDataService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Referer", "https://finance.sina.com.cn/");

            // 设置更新间隔为1秒
            _updateTimer = new System.Timers.Timer(1000);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.Start();
        }

        /// <summary>
        /// 转换股票代码格式
        /// 将常见格式转换为新浪财经格式
        /// </summary>
        private string ConvertSymbolToSina(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return symbol;

            // 去除可能的前缀和后缀
            symbol = symbol.ToUpper().Replace(".SS", "").Replace(".SZ", "").Replace(".HK", "");

            // 判断市场并添加前缀
            if (symbol.Length == 6 && symbol.All(char.IsDigit))
            {
                // 沪深股票代码
                if (symbol.StartsWith("6") || symbol.StartsWith("9"))
                    return $"sh{symbol}"; // 上海
                else if (symbol.StartsWith("0") || symbol.StartsWith("2") || symbol.StartsWith("3"))
                    return $"sz{symbol}"; // 深圳
            }
            else if (symbol.Length <= 5 && symbol.All(char.IsDigit))
            {
                // 港股
                return $"hk{symbol.PadLeft(5, '0')}";
            }
            else
            {
                // 美股或其他，尝试直接使用
                return symbol.ToLower();
            }

            return symbol.ToLower();
        }

        /// <summary>
        /// 解析新浪财经数据
        /// </summary>
        private Level1Data ParseSinaData(string symbol, string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data) || !data.Contains("="))
                    return null;

                var equalIndex = data.IndexOf('=');
                var dataString = data.Substring(equalIndex + 1).Trim(' ', '"', ';');
                var fields = dataString.Split(',');

                if (fields.Length < 30) // 新浪数据至少包含30个字段
                    return null;

                var level1Data = new Level1Data
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Now,
                    LastPrice = ParseDecimal(fields[3]),  // 当前价
                    Open = ParseDecimal(fields[1]),       // 开盘价
                    High = ParseDecimal(fields[4]),       // 最高价
                    Low = ParseDecimal(fields[5]),        // 最低价
                    PreClose = ParseDecimal(fields[2]),   // 昨收价
                    Volume = ParseDecimal(fields[8]),     // 成交量
                    Turnover = ParseDecimal(fields[9]),   // 成交额
                    BidPrice1 = ParseDecimal(fields[11]), // 买一价
                    BidVolume1 = ParseDecimal(fields[10]), // 买一量
                    AskPrice1 = ParseDecimal(fields[21]), // 卖一价
                    AskVolume1 = ParseDecimal(fields[20]) // 卖一量
                };

                return level1Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Sina data for {symbol}: {ex.Message}");
                return null;
            }
        }

        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            if (decimal.TryParse(value, out var result))
                return result;

            return 0;
        }

        public async Task<Level1Data> GetLevel1DataAsync(string symbol)
        {
            try
            {
                var sinaSymbol = ConvertSymbolToSina(symbol);
                var url = string.Format(SinaQuoteUrl, sinaSymbol);

                var response = await _httpClient.GetStringAsync(url);

                // 设置编码为GBK（新浪返回的是GBK编码）
                var gbkBytes = Encoding.GetEncoding("GBK").GetBytes(response);
                var utf8Response = Encoding.UTF8.GetString(Encoding.Convert(Encoding.GetEncoding("GBK"), Encoding.UTF8, gbkBytes));

                var data = ParseSinaData(symbol, utf8Response);

                if (data != null)
                {
                    lock (_lockObject)
                    {
                        _level1Cache[symbol] = data;
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching data from Sina for {symbol}: {ex.Message}");
                return null;
            }
        }

        public void SubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            lock (_lockObject)
            {
                if (!_level1Subscribers.ContainsKey(symbol))
                {
                    _level1Subscribers[symbol] = new List<Action<Level1Data>>();
                }

                if (!_level1Subscribers[symbol].Contains(callback))
                {
                    _level1Subscribers[symbol].Add(callback);
                }

                // 初次订阅时获取数据
                Task.Run(async () =>
                {
                    var data = await GetLevel1DataAsync(symbol);
                    if (data != null)
                    {
                        callback(data);
                    }
                });
            }
        }

        public void UnsubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            lock (_lockObject)
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
        }

        public async Task<List<Candlestick>> GetHistoricalCandlesticksAsync(string symbol, DateTime startTime, DateTime endTime, TimeSpan period)
        {
            // 新浪财经的K线数据接口相对复杂，这里提供基础实现
            // 实际使用中可能需要调用更专业的数据接口
            return await GetLatestCandlesticksAsync(symbol, 100, period);
        }

        public async Task<List<Candlestick>> GetLatestCandlesticksAsync(string symbol, int count, TimeSpan period)
        {
            try
            {
                // 检查缓存
                lock (_lockObject)
                {
                    if (_candleCache.ContainsKey(symbol))
                    {
                        var cached = _candleCache[symbol];
                        if (cached.Count > 0 && DateTime.Now - cached.Last().Timestamp < TimeSpan.FromMinutes(5))
                        {
                            return cached.TakeLast(count).ToList();
                        }
                    }
                }

                // 生成模拟K线数据（基于当前价格）
                var currentData = await GetLevel1DataAsync(symbol);
                if (currentData == null)
                    return new List<Candlestick>();

                var candles = GenerateSimulatedCandles(symbol, currentData, count, period);

                lock (_lockObject)
                {
                    _candleCache[symbol] = candles;
                }

                return candles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching K-line data for {symbol}: {ex.Message}");
                return new List<Candlestick>();
            }
        }

        /// <summary>
        /// 生成模拟K线数据（基于实时价格）
        /// </summary>
        private List<Candlestick> GenerateSimulatedCandles(string symbol, Level1Data currentData, int count, TimeSpan period)
        {
            var candles = new List<Candlestick>();
            var random = new Random();
            var currentPrice = currentData.LastPrice;
            var baseTime = DateTime.Now.Date;

            // 如果是分钟级别，从今天开始往前推
            if (period.TotalMinutes <= 60)
            {
                baseTime = DateTime.Now.AddMinutes(-count * period.TotalMinutes);
            }
            else
            {
                baseTime = DateTime.Now.AddDays(-count);
            }

            for (int i = 0; i < count; i++)
            {
                var candleTime = baseTime.Add(TimeSpan.FromMinutes(i * period.TotalMinutes));

                // 基于当前价格生成历史价格（随机波动）
                var volatility = 0.02m; // 2%波动率
                var change = (decimal)(random.NextDouble() * 2 - 1) * volatility;
                var basePrice = currentPrice * (1 - (count - i) * 0.001m); // 历史价格略低于当前价格

                var open = basePrice * (1 + change);
                var changeRange = (decimal)(random.NextDouble() * 0.01); // 1%范围内波动
                var high = open * (1 + changeRange);
                var low = open * (1 - changeRange);
                var close = low + (high - low) * (decimal)random.NextDouble();

                candles.Add(new Candlestick
                {
                    Symbol = symbol,
                    Timestamp = candleTime,
                    Period = period,
                    Open = Math.Max(0.01m, open),
                    High = Math.Max(0.01m, high),
                    Low = Math.Max(0.01m, low),
                    Close = Math.Max(0.01m, close),
                    Volume = random.Next(1000, 100000)
                });
            }

            return candles;
        }

        private async void OnUpdateTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var symbolsToUpdate = new List<string>();

            lock (_lockObject)
            {
                symbolsToUpdate.AddRange(_level1Subscribers.Keys);
            }

            foreach (var symbol in symbolsToUpdate)
            {
                try
                {
                    var data = await GetLevel1DataAsync(symbol);
                    if (data != null)
                    {
                        // 通知订阅者
                        List<Action<Level1Data>> callbacks;
                        lock (_lockObject)
                        {
                            if (!_level1Subscribers.TryGetValue(symbol, out callbacks))
                                continue;
                            callbacks = callbacks.ToList(); // 创建副本避免并发问题
                        }

                        foreach (var callback in callbacks)
                        {
                            try
                            {
                                callback(data);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error in callback for {symbol}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating data for {symbol}: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
