using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 东方财富市场数据服务
    /// </summary>
    public class EastmoneyMarketDataService : IMarketDataService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, Level1Data> _level1Cache = new Dictionary<string, Level1Data>();
        private readonly Dictionary<string, List<Action<Level1Data>>> _level1Subscribers = new Dictionary<string, List<Action<Level1Data>>>();
        private readonly Dictionary<string, List<Candlestick>> _candleCache = new Dictionary<string, List<Candlestick>>();
        private readonly System.Timers.Timer _updateTimer;
        private readonly object _lockObject = new object();

        // 东方财富接口URL
        private const string EmQuoteUrl = "http://push2.eastmoney.com/api/qt/stock/get?fields=f43,f44,f45,f46,f47,f48,f49,f50,f51,f52,f53,f54,f55,f56,f57,f58&secid={0}";
        private const string EmKLineUrl = "http://push2his.eastmoney.com/api/qt/stock/kline/get?fields1=f1,f2,f3,f4,f5,f6&fields2=f51,f52,f53,f54,f55,f56,f57,f58,f59,f60,f61&klt={0}&fqt=1&secid={1}&end={2}";

        public EastmoneyMarketDataService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Referer", "http://quote.eastmoney.com/");

            // 设置更新间隔为2秒（东方财富限制更严格）
            _updateTimer = new System.Timers.Timer(2000);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.Start();
        }

        /// <summary>
        /// 转换股票代码为东方财富格式
        /// </summary>
        private string ConvertSymbolToEastmoney(string symbol)
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
                    return $"1.{symbol}"; // 上海：1
                else if (symbol.StartsWith("0") || symbol.StartsWith("2") || symbol.StartsWith("3"))
                    return $"0.{symbol}"; // 深圳：0
            }
            else if (symbol.Length <= 5 && symbol.All(char.IsDigit))
            {
                // 港股
                return $"116.{symbol.PadLeft(5, '0')}"; // 港股：116
            }

            return $"1.{symbol}"; // 默认上海
        }

        public async Task<Level1Data> GetLevel1DataAsync(string symbol)
        {
            try
            {
                var emSymbol = ConvertSymbolToEastmoney(symbol);
                var url = string.Format(EmQuoteUrl, emSymbol);

                var response = await _httpClient.GetStringAsync(url);
                var data = ParseEastmoneyData(symbol, response);

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
                Console.WriteLine($"Error fetching data from Eastmoney for {symbol}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 解析东方财富数据
        /// </summary>
        private Level1Data ParseEastmoneyData(string symbol, string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                if (!root.TryGetProperty("data", out var dataElement))
                    return null;

                var level1Data = new Level1Data
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Now,
                    LastPrice = GetDecimalFromJson(dataElement, "f43") / 100m,    // 当前价（分为单位）
                    Open = GetDecimalFromJson(dataElement, "f46") / 100m,         // 开盘价
                    High = GetDecimalFromJson(dataElement, "f44") / 100m,         // 最高价  
                    Low = GetDecimalFromJson(dataElement, "f45") / 100m,          // 最低价
                    PreClose = GetDecimalFromJson(dataElement, "f60") / 100m,     // 昨收价
                    Volume = GetDecimalFromJson(dataElement, "f47"),              // 成交量
                    Turnover = GetDecimalFromJson(dataElement, "f48"),            // 成交额
                    BidPrice1 = GetDecimalFromJson(dataElement, "f49") / 100m,    // 买一价
                    BidVolume1 = GetDecimalFromJson(dataElement, "f50"),          // 买一量
                    AskPrice1 = GetDecimalFromJson(dataElement, "f51") / 100m,    // 卖一价
                    AskVolume1 = GetDecimalFromJson(dataElement, "f52")           // 卖一量
                };

                return level1Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Eastmoney data for {symbol}: {ex.Message}");
                return null;
            }
        }

        private decimal GetDecimalFromJson(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number)
                {
                    return property.GetDecimal();
                }
                else if (property.ValueKind == JsonValueKind.String)
                {
                    if (decimal.TryParse(property.GetString(), out var result))
                        return result;
                }
            }

            return 0;
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
            try
            {
                var emSymbol = ConvertSymbolToEastmoney(symbol);
                var klt = ConvertPeriodToKlt(period);
                var endDate = endTime.ToString("yyyyMMdd");

                var url = string.Format(EmKLineUrl, klt, emSymbol, endDate);
                var response = await _httpClient.GetStringAsync(url);

                return ParseEastmoneyKLineData(symbol, response, period);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching K-line data from Eastmoney for {symbol}: {ex.Message}");
                return new List<Candlestick>();
            }
        }

        public async Task<List<Candlestick>> GetLatestCandlesticksAsync(string symbol, int count, TimeSpan period)
        {
            var endTime = DateTime.Now;
            var startTime = endTime.AddDays(-Math.Max(count * 2, 100)); // 获取足够的历史数据

            var candles = await GetHistoricalCandlesticksAsync(symbol, startTime, endTime, period);
            return candles.TakeLast(count).ToList();
        }

        /// <summary>
        /// 转换时间周期为东方财富的klt参数
        /// </summary>
        private string ConvertPeriodToKlt(TimeSpan period)
        {
            if (period.TotalMinutes <= 1)
                return "1";   // 1分钟
            else if (period.TotalMinutes <= 5)
                return "5";   // 5分钟
            else if (period.TotalMinutes <= 15)
                return "15";  // 15分钟
            else if (period.TotalMinutes <= 30)
                return "30";  // 30分钟
            else if (period.TotalMinutes <= 60)
                return "60";  // 60分钟
            else if (period.TotalDays <= 1)
                return "101"; // 日K
            else if (period.TotalDays <= 7)
                return "102"; // 周K
            else
                return "103"; // 月K
        }

        /// <summary>
        /// 解析东方财富K线数据
        /// </summary>
        private List<Candlestick> ParseEastmoneyKLineData(string symbol, string jsonResponse, TimeSpan period)
        {
            var candles = new List<Candlestick>();

            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                if (!root.TryGetProperty("data", out var dataElement) ||
                    !dataElement.TryGetProperty("klines", out var klines))
                    return candles;

                foreach (var klineElement in klines.EnumerateArray())
                {
                    var klineData = klineElement.GetString().Split(',');
                    if (klineData.Length < 6)
                        continue;

                    if (DateTime.TryParse(klineData[0], out var timestamp) &&
                        decimal.TryParse(klineData[1], out var open) &&
                        decimal.TryParse(klineData[2], out var close) &&
                        decimal.TryParse(klineData[3], out var high) &&
                        decimal.TryParse(klineData[4], out var low) &&
                        decimal.TryParse(klineData[5], out var volume))
                    {
                        candles.Add(new Candlestick
                        {
                            Symbol = symbol,
                            Timestamp = timestamp,
                            Period = period,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            Volume = volume
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Eastmoney K-line data for {symbol}: {ex.Message}");
            }

            return candles.OrderBy(c => c.Timestamp).ToList();
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

                // 添加延迟避免请求过于频繁
                await Task.Delay(100);
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
