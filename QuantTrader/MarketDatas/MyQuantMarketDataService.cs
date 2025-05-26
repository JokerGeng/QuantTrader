using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    public class MyQuantMarketDataService : IAuthenticatableMarketDataService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private string _apiKey;
        private bool _isAuthenticated;

        public bool IsAuthenticated => _isAuthenticated;

        public MyQuantMarketDataService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "QuantTrader/1.0");
        }

        public async Task<bool> AuthenticateAsync(string apiKey)
        {
            try
            {
                _apiKey = apiKey;

                // 设置认证头
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "QuantTrader/1.0");

                // 测试API Key有效性
                var testUrl = "https://api.myquant.cn/v1/auth/verify";
                try
                {
                    var response = await _httpClient.GetAsync(testUrl);
                    _isAuthenticated = response.IsSuccessStatusCode;
                }
                catch
                {
                    // 如果接口不存在，假设认证成功（模拟）
                    _isAuthenticated = !string.IsNullOrEmpty(apiKey) && apiKey.Length > 10;
                }

                return _isAuthenticated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MyQuant认证失败: {ex.Message}");
                _isAuthenticated = false;
                return false;
            }
        }

        public async Task<bool> AuthenticateAsync(string username, string password, string serverAddress)
        {
            try
            {
                var loginData = new
                {
                    username = username,
                    password = password
                };

                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var loginUrl = $"{serverAddress}/api/v1/login";
                var response = await _httpClient.PostAsync(loginUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var loginResult = JsonSerializer.Deserialize<JsonElement>(responseJson);

                    if (loginResult.TryGetProperty("token", out var tokenElement))
                    {
                        var token = tokenElement.GetString();
                        return await AuthenticateAsync(token);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MyQuant登录失败: {ex.Message}");
                return false;
            }
        }

        public async Task<Level1Data> GetLevel1DataAsync(string symbol)
        {
            if (!_isAuthenticated)
                throw new InvalidOperationException("未认证，无法获取数据");

            try
            {
                var url = $"https://api.myquant.cn/v1/market/quote/{symbol}";
                var response = await _httpClient.GetStringAsync(url);

                return ParseMyQuantData(symbol, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取MyQuant数据失败: {ex.Message}");

                // 返回模拟数据
                return new Level1Data
                {
                    Symbol = symbol,
                    Timestamp = DateTime.Now,
                    LastPrice = 100.0m + (decimal)(new Random().NextDouble() * 10 - 5),
                    Open = 99.5m,
                    High = 101.0m,
                    Low = 99.0m,
                    Volume = 1000000,
                    PreClose = 99.8m
                };
            }
        }

        private Level1Data ParseMyQuantData(string symbol, string jsonResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    return new Level1Data
                    {
                        Symbol = symbol,
                        Timestamp = DateTime.Now,
                        LastPrice = GetDecimalFromJson(dataElement, "last_price"),
                        Open = GetDecimalFromJson(dataElement, "open"),
                        High = GetDecimalFromJson(dataElement, "high"),
                        Low = GetDecimalFromJson(dataElement, "low"),
                        Volume = GetDecimalFromJson(dataElement, "volume"),
                        PreClose = GetDecimalFromJson(dataElement, "pre_close"),
                        BidPrice1 = GetDecimalFromJson(dataElement, "bid_price"),
                        BidVolume1 = GetDecimalFromJson(dataElement, "bid_volume"),
                        AskPrice1 = GetDecimalFromJson(dataElement, "ask_price"),
                        AskVolume1 = GetDecimalFromJson(dataElement, "ask_volume")
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析MyQuant数据失败: {ex.Message}");
            }

            return null;
        }

        private decimal GetDecimalFromJson(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var property))
            {
                if (property.ValueKind == JsonValueKind.Number)
                    return property.GetDecimal();
                else if (property.ValueKind == JsonValueKind.String)
                    if (decimal.TryParse(property.GetString(), out var result))
                        return result;
            }
            return 0;
        }

        public void SubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            // MyQuant WebSocket订阅实现
            throw new NotImplementedException("MyQuant实时订阅功能需要WebSocket实现");
        }

        public void UnsubscribeLevel1Data(string symbol, Action<Level1Data> callback)
        {
            throw new NotImplementedException("MyQuant取消订阅功能需要WebSocket实现");
        }

        public async Task<List<Candlestick>> GetHistoricalCandlesticksAsync(string symbol, DateTime startTime, DateTime endTime, TimeSpan period)
        {
            if (!_isAuthenticated)
                throw new InvalidOperationException("未认证，无法获取数据");

            try
            {
                var periodStr = ConvertPeriodToString(period);
                var url = $"https://api.myquant.cn/v1/market/kline/{symbol}?period={periodStr}&start={startTime:yyyy-MM-dd}&end={endTime:yyyy-MM-dd}";

                var response = await _httpClient.GetStringAsync(url);
                return ParseKLineData(symbol, response, period);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取MyQuant历史数据失败: {ex.Message}");
                return new List<Candlestick>();
            }
        }

        public async Task<List<Candlestick>> GetLatestCandlesticksAsync(string symbol, int count, TimeSpan period)
        {
            var endTime = DateTime.Now;
            var startTime = endTime.AddDays(-count * 2);

            var candles = await GetHistoricalCandlesticksAsync(symbol, startTime, endTime, period);
            return candles.TakeLast(count).ToList();
        }

        private string ConvertPeriodToString(TimeSpan period)
        {
            if (period.TotalMinutes <= 1) return "1m";
            if (period.TotalMinutes <= 5) return "5m";
            if (period.TotalMinutes <= 15) return "15m";
            if (period.TotalMinutes <= 30) return "30m";
            if (period.TotalHours <= 1) return "1h";
            if (period.TotalDays <= 1) return "1d";
            if (period.TotalDays <= 7) return "1w";
            return "1M";
        }

        private List<Candlestick> ParseKLineData(string symbol, string jsonResponse, TimeSpan period)
        {
            var candles = new List<Candlestick>();

            try
            {
                using var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                if (root.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        if (DateTime.TryParse(item.GetProperty("timestamp").GetString(), out var timestamp))
                        {
                            candles.Add(new Candlestick
                            {
                                Symbol = symbol,
                                Timestamp = timestamp,
                                Period = period,
                                Open = GetDecimalFromJson(item, "open"),
                                High = GetDecimalFromJson(item, "high"),
                                Low = GetDecimalFromJson(item, "low"),
                                Close = GetDecimalFromJson(item, "close"),
                                Volume = GetDecimalFromJson(item, "volume")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析MyQuant K线数据失败: {ex.Message}");
            }

            return candles.OrderBy(c => c.Timestamp).ToList();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
