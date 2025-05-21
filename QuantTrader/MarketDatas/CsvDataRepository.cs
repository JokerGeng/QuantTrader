using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using QuantTrader.Models;

namespace QuantTrader.MarketDatas
{
    public class CsvDataRepository : IDataRepository, IDisposable
    {
        private readonly string _baseDirectory;

        // 文件锁定字典 - 按文件路径保存锁对象
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        // 日志缓冲区 - 按日期和文件类型缓存日志，减少文件读写次数
        private readonly ConcurrentDictionary<string, List<StrategyLogRecord>> _logBuffer = new ConcurrentDictionary<string, List<StrategyLogRecord>>();

        // 日志刷新计时器 - 定期将缓存的日志写入文件
        private readonly Timer _logFlushTimer;

        // 最大缓冲日志数量，超过此数量会触发写入
        private const int MaxBufferedLogCount = 100;

        // 文件重试配置
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 100;

        public CsvDataRepository(string baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QuantTrader",
                "Data");

            // 确保目录存在
            Directory.CreateDirectory(_baseDirectory);
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "Orders"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "Accounts"));
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "Strategies"));

            // 初始化日志刷新计时器 - 每5秒刷新一次
            _logFlushTimer = new Timer(FlushAllLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        // 获取文件锁 - 如果锁不存在则创建
        private SemaphoreSlim GetFileLock(string filePath)
        {
            return _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        }

        // 通用文件写入方法 - 带有重试逻辑
        private async Task WriteToFileAsync<T>(string filePath, IEnumerable<T> records, bool append = true)
        {
            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync();

            try
            {
                var attempts = 0;
                var success = false;

                // 创建目录(如果不存在)
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                while (!success && attempts < MaxRetryAttempts)
                {
                    try
                    {
                        var fileExists = File.Exists(filePath);

                        using (var stream = new FileStream(filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream))
                        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
                        {
                            if (!fileExists || !append)
                            {
                                // 写入表头
                                csv.WriteHeader<T>();
                                csv.NextRecord();
                            }

                            // 写入记录
                            foreach (var record in records)
                            {
                                csv.WriteRecord(record);
                                csv.NextRecord();
                            }

                            await writer.FlushAsync();
                        }

                        success = true;
                    }
                    catch (IOException) when (++attempts < MaxRetryAttempts)
                    {
                        // 捕获IO异常并重试
                        await Task.Delay(RetryDelayMs * attempts);
                    }
                }

                if (!success)
                {
                    throw new IOException($"Unable to write to file '{filePath}' after {MaxRetryAttempts} attempts.");
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        // 通用文件读取方法 - 带有重试逻辑
        private async Task<List<T>> ReadFromFileAsync<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<T>();

            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync();

            try
            {
                var attempts = 0;
                while (attempts < MaxRetryAttempts)
                {
                    try
                    {
                        using (var reader = new StreamReader(filePath))
                        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                        {
                            return await Task.Run(() => csv.GetRecords<T>().ToList());
                        }
                    }
                    catch (IOException) when (++attempts < MaxRetryAttempts)
                    {
                        // 捕获IO异常并重试
                        await Task.Delay(RetryDelayMs * attempts);
                    }
                }

                throw new IOException($"Unable to read from file '{filePath}' after {MaxRetryAttempts} attempts.");
            }
            finally
            {
                fileLock.Release();
            }
        }

        // 优化的日志写入方法 - 使用缓冲区
        public async Task LogStrategyExecutionAsync(string strategyId, string message, DateTime timestamp)
        {
            var dateKey = timestamp.ToString("yyyyMMdd");
            var logRecord = new StrategyLogRecord
            {
                Timestamp = timestamp,
                StrategyId = strategyId,
                Message = message
            };

            // 添加到缓冲区
            var logs = _logBuffer.GetOrAdd(dateKey, _ => new List<StrategyLogRecord>());

            lock (logs)
            {
                logs.Add(logRecord);

                // 如果缓冲区达到阈值，立即刷新
                if (logs.Count >= MaxBufferedLogCount)
                {
                    // 将日志标记为需要刷新，但在后台刷新，不阻塞当前操作
                    Task.Run(() => FlushLogsAsync(dateKey));
                }
            }
        }

        // 刷新特定日期的日志到文件
        private async Task FlushLogsAsync(string dateKey)
        {
            if (!_logBuffer.TryGetValue(dateKey, out var logs) || logs.Count == 0)
                return;

            List<StrategyLogRecord> logsToWrite;

            lock (logs)
            {
                // 创建日志副本并清空缓冲区
                logsToWrite = new List<StrategyLogRecord>(logs);
                logs.Clear();
            }

            // 写入日志到文件
            var filePath = Path.Combine(_baseDirectory, "Strategies", $"strategy_log_{dateKey}.csv");
            await WriteToFileAsync(filePath, logsToWrite, true);
        }

        // 刷新所有日志到文件 (计时器回调方法)
        private void FlushAllLogs(object state)
        {
            // 获取需要刷新的日期
            var dateKeys = _logBuffer.Keys.ToList();

            foreach (var dateKey in dateKeys)
            {
                // 使用Task.Run而不是await来避免在Timer回调中使用async
                Task.Run(() => FlushLogsAsync(dateKey)).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        // 记录错误，但不抛出异常
                        Console.WriteLine($"Error flushing logs: {t.Exception?.InnerException?.Message}");
                    }
                });
            }
        }

        public async Task SaveOrderAsync(Order order)
        {
            var filePath = Path.Combine(_baseDirectory, "Orders", $"orders_{order.CreateTime.ToString("yyyyMMdd")}.csv");

            // 将订单转换为记录格式
            var record = new OrderRecord
            {
                OrderId = order.OrderId,
                Symbol = order.Symbol,
                Direction = order.Direction.ToString(),
                Type = order.Type.ToString(),
                Price = order.Price,
                StopPrice = order.StopPrice,
                Quantity = order.Quantity,
                FilledQuantity = order.FilledQuantity,
                Status = order.Status.ToString(),
                CreateTime = order.CreateTime,
                UpdateTime = order.UpdateTime,
                StrategyId = order.StrategyId,
                Message = order.Message,
                AverageFilledPrice = order.AverageFilledPrice
            };

            // 写入单个记录
            await WriteToFileAsync(filePath, new[] { record });
        }

        public async Task<List<Order>> GetOrderHistoryAsync(DateTime startTime, DateTime endTime, string symbol = null)
        {
            var orders = new List<Order>();

            // 获取日期范围内的所有文件
            var startDate = startTime.Date;
            var endDate = endTime.Date;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var filePath = Path.Combine(_baseDirectory, "Orders", $"orders_{date.ToString("yyyyMMdd")}.csv");

                if (!File.Exists(filePath))
                    continue;

                // 读取记录
                var records = await ReadFromFileAsync<OrderRecord>(filePath);

                // 筛选符合条件的订单
                var filteredRecords = records
                    .Where(r => r.CreateTime >= startTime && r.CreateTime <= endTime &&
                               (symbol == null || r.Symbol == symbol))
                    .ToList();

                // 转换为订单对象
                orders.AddRange(filteredRecords.Select(r => new Order
                {
                    OrderId = r.OrderId,
                    Symbol = r.Symbol,
                    Direction = Enum.Parse<OrderDirection>(r.Direction),
                    Type = Enum.Parse<OrderType>(r.Type),
                    Price = r.Price,
                    StopPrice = r.StopPrice,
                    Quantity = r.Quantity,
                    FilledQuantity = r.FilledQuantity,
                    Status = Enum.Parse<OrderStatus>(r.Status),
                    CreateTime = r.CreateTime,
                    UpdateTime = r.UpdateTime,
                    StrategyId = r.StrategyId,
                    Message = r.Message,
                    AverageFilledPrice = r.AverageFilledPrice
                }));
            }

            // 按创建时间排序
            return orders.OrderBy(o => o.CreateTime).ToList();
        }

        public async Task SaveAccountSnapshotAsync(Account account, DateTime timestamp)
        {
            // 保存账户基本信息
            var accountFilePath = Path.Combine(_baseDirectory, "Accounts", $"account_{timestamp.ToString("yyyyMMdd")}.csv");
            var accountRecord = new AccountRecord
            {
                Timestamp = timestamp,
                AccountId = account.AccountId,
                Cash = account.Cash,
                TotalAssetValue = account.TotalAssetValue,
                MarginUsed = account.MarginUsed,
                MarginAvailable = account.MarginAvailable
            };

            await WriteToFileAsync(accountFilePath, new[] { accountRecord });

            // 保存持仓信息
            if (account.Positions.Count > 0)
            {
                var positionsFilePath = Path.Combine(_baseDirectory, "Accounts", $"positions_{timestamp.ToString("yyyyMMdd")}.csv");

                // 创建持仓记录列表
                var positionRecords = account.Positions.Values.Select(position => new PositionRecord
                {
                    Timestamp = timestamp,
                    AccountId = account.AccountId,
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AverageCost = position.AverageCost,
                    CurrentPrice = position.CurrentPrice,
                    MarketValue = position.MarketValue,
                    UnrealizedPnL = position.UnrealizedPnL
                }).ToList();

                await WriteToFileAsync(positionsFilePath, positionRecords);
            }
        }

        public async Task<List<Tuple<DateTime, Account>>> GetAccountHistoryAsync(DateTime startTime, DateTime endTime)
        {
            var result = new List<Tuple<DateTime, Account>>();

            // 获取日期范围内的所有文件
            var startDate = startTime.Date;
            var endDate = endTime.Date;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var accountFilePath = Path.Combine(_baseDirectory, "Accounts", $"account_{date.ToString("yyyyMMdd")}.csv");
                var positionsFilePath = Path.Combine(_baseDirectory, "Accounts", $"positions_{date.ToString("yyyyMMdd")}.csv");

                if (!File.Exists(accountFilePath))
                    continue;

                // 读取账户记录
                var accountRecords = await ReadFromFileAsync<AccountRecord>(accountFilePath);
                var filteredRecords = accountRecords.Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime).ToList();

                // 读取持仓记录
                Dictionary<DateTime, List<PositionRecord>> positionRecordsByTime = new Dictionary<DateTime, List<PositionRecord>>();

                if (File.Exists(positionsFilePath))
                {
                    var positions = await ReadFromFileAsync<PositionRecord>(positionsFilePath);
                    var filteredPositions = positions.Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime).ToList();

                    foreach (var position in filteredPositions)
                    {
                        if (!positionRecordsByTime.ContainsKey(position.Timestamp))
                        {
                            positionRecordsByTime[position.Timestamp] = new List<PositionRecord>();
                        }
                        positionRecordsByTime[position.Timestamp].Add(position);
                    }
                }

                // 组合账户和持仓记录
                foreach (var accountRecord in filteredRecords)
                {
                    var account = new Account(accountRecord.AccountId, accountRecord.Cash)
                    {
                        TotalAssetValue = accountRecord.TotalAssetValue,
                        MarginUsed = accountRecord.MarginUsed,
                        MarginAvailable = accountRecord.MarginAvailable
                    };

                    // 添加持仓信息
                    if (positionRecordsByTime.TryGetValue(accountRecord.Timestamp, out var positions))
                    {
                        foreach (var positionRecord in positions)
                        {
                            var position = new Position(
                                positionRecord.Symbol,
                                positionRecord.Quantity,
                                positionRecord.AverageCost,
                                positionRecord.CurrentPrice
                            );
                            account.UpdatePosition(position);
                        }
                    }

                    result.Add(new Tuple<DateTime, Account>(accountRecord.Timestamp, account));
                }
            }

            // 按时间排序
            return result.OrderBy(t => t.Item1).ToList();
        }

        public async Task<List<Tuple<DateTime, string>>> GetStrategyLogsAsync(string strategyId, DateTime startTime, DateTime endTime)
        {
            var result = new List<Tuple<DateTime, string>>();

            // 先刷新所有缓冲日志
            var dateKeys = _logBuffer.Keys.ToList();
            foreach (var dateKey in dateKeys)
            {
                await FlushLogsAsync(dateKey);
            }

            // 获取日期范围内的所有文件
            var startDate = startTime.Date;
            var endDate = endTime.Date;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var filePath = Path.Combine(_baseDirectory, "Strategies", $"strategy_log_{date.ToString("yyyyMMdd")}.csv");

                if (!File.Exists(filePath))
                    continue;

                // 读取日志记录
                var logs = await ReadFromFileAsync<StrategyLogRecord>(filePath);

                // 筛选日志
                var filteredLogs = logs
                    .Where(r => r.StrategyId == strategyId && r.Timestamp >= startTime && r.Timestamp <= endTime)
                    .ToList();

                result.AddRange(filteredLogs.Select(log => new Tuple<DateTime, string>(log.Timestamp, log.Message)));
            }

            // 按时间排序
            return result.OrderBy(t => t.Item1).ToList();
        }

        public void Dispose()
        {
            // 停止计时器
            _logFlushTimer?.Dispose();

            // 刷新所有剩余日志
            var dateKeys = _logBuffer.Keys.ToList();
            foreach (var dateKey in dateKeys)
            {
                FlushLogsAsync(dateKey).Wait();
            }

            // 释放所有文件锁
            foreach (var fileLock in _fileLocks.Values)
            {
                fileLock.Dispose();
            }

            _fileLocks.Clear();
        }

        #region 记录类型

        private class OrderRecord
        {
            public string OrderId { get; set; }
            public string Symbol { get; set; }
            public string Direction { get; set; }
            public string Type { get; set; }
            public decimal Price { get; set; }
            public decimal StopPrice { get; set; }
            public int Quantity { get; set; }
            public int FilledQuantity { get; set; }
            public string Status { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime UpdateTime { get; set; }
            public string StrategyId { get; set; }
            public string Message { get; set; }
            public decimal AverageFilledPrice { get; set; }
        }

        private class AccountRecord
        {
            public DateTime Timestamp { get; set; }
            public string AccountId { get; set; }
            public decimal Cash { get; set; }
            public decimal TotalAssetValue { get; set; }
            public decimal MarginUsed { get; set; }
            public decimal MarginAvailable { get; set; }
        }

        private class PositionRecord
        {
            public DateTime Timestamp { get; set; }
            public string AccountId { get; set; }
            public string Symbol { get; set; }
            public int Quantity { get; set; }
            public decimal AverageCost { get; set; }
            public decimal CurrentPrice { get; set; }
            public decimal MarketValue { get; set; }
            public decimal UnrealizedPnL { get; set; }
        }

        private class StrategyLogRecord
        {
            public DateTime Timestamp { get; set; }
            public string StrategyId { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}