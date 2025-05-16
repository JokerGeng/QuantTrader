using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using QuantTrader.Models;

namespace QuantTrader.MarketDatas
{
    public class CsvDataRepository : IDataRepository
    {
        private readonly string _baseDirectory;

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
        }

        public async Task SaveOrderAsync(Order order)
        {
            var filePath = Path.Combine(_baseDirectory, "Orders", $"orders_{order.CreateTime.ToString("yyyyMMdd")}.csv");
            var fileExists = File.Exists(filePath);

            using (var writer = new StreamWriter(filePath, true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                if (!fileExists)
                {
                    // 写入表头
                    csv.WriteHeader<OrderRecord>();
                    csv.NextRecord();
                }

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

                // 写入记录
                csv.WriteRecord(record);
                csv.NextRecord();

                await writer.FlushAsync();
            }
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

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    // 读取所有记录
                    var records = await Task.Run(() => csv.GetRecords<OrderRecord>().ToList());

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
            }

            // 按创建时间排序
            return orders.OrderBy(o => o.CreateTime).ToList();
        }

        public async Task SaveAccountSnapshotAsync(Account account, DateTime timestamp)
        {
            var filePath = Path.Combine(_baseDirectory, "Accounts", $"account_{timestamp.ToString("yyyyMMdd")}.csv");
            var fileExists = File.Exists(filePath);

            using (var writer = new StreamWriter(filePath, true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                if (!fileExists)
                {
                    // 写入表头
                    csv.WriteHeader<AccountRecord>();
                    csv.NextRecord();
                }

                // 保存账户基本信息
                var record = new AccountRecord
                {
                    Timestamp = timestamp,
                    AccountId = account.AccountId,
                    Cash = account.Cash,
                    TotalAssetValue = account.TotalAssetValue,
                    MarginUsed = account.MarginUsed,
                    MarginAvailable = account.MarginAvailable
                };

                // 写入记录
                csv.WriteRecord(record);
                csv.NextRecord();

                await writer.FlushAsync();
            }

            // 保存持仓信息
            var positionsPath = Path.Combine(_baseDirectory, "Accounts", $"positions_{timestamp.ToString("yyyyMMdd")}.csv");
            var positionsFileExists = File.Exists(positionsPath);

            using (var writer = new StreamWriter(positionsPath, true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                if (!positionsFileExists)
                {
                    // 写入表头
                    csv.WriteHeader<PositionRecord>();
                    csv.NextRecord();
                }

                // 写入每个持仓记录
                foreach (var position in account.Positions.Values)
                {
                    var record = new PositionRecord
                    {
                        Timestamp = timestamp,
                        AccountId = account.AccountId,
                        Symbol = position.Symbol,
                        Quantity = position.Quantity,
                        AverageCost = position.AverageCost,
                        CurrentPrice = position.CurrentPrice,
                        MarketValue = position.MarketValue,
                        UnrealizedPnL = position.UnrealizedPnL
                    };

                    csv.WriteRecord(record);
                    csv.NextRecord();
                }

                await writer.FlushAsync();
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
                List<AccountRecord> accountRecords;
                using (var reader = new StreamReader(accountFilePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    accountRecords = await Task.Run(() => csv.GetRecords<AccountRecord>()
                        .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                        .ToList());
                }
                Dictionary<DateTime, List<PositionRecord>> positionRecordsByTime = new Dictionary<DateTime, List<PositionRecord>>();

                if (File.Exists(positionsFilePath))
                {
                    using (var reader = new StreamReader(positionsFilePath))
                    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                    {
                        var positions = await Task.Run(() => csv.GetRecords<PositionRecord>()
                            .Where(r => r.Timestamp >= startTime && r.Timestamp <= endTime)
                            .ToList());

                        foreach (var position in positions)
                        {
                            if (!positionRecordsByTime.ContainsKey(position.Timestamp))
                            {
                                positionRecordsByTime[position.Timestamp] = new List<PositionRecord>();
                            }
                            positionRecordsByTime[position.Timestamp].Add(position);
                        }
                    }
                }

                // 组合账户和持仓记录
                foreach (var accountRecord in accountRecords)
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

        public async Task LogStrategyExecutionAsync(string strategyId, string message, DateTime timestamp)
        {
            var filePath = Path.Combine(_baseDirectory, "Strategies", $"strategy_log_{timestamp.ToString("yyyyMMdd")}.csv");
            var fileExists = File.Exists(filePath);

            using (var writer = new StreamWriter(filePath, true))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                if (!fileExists)
                {
                    // 写入表头
                    csv.WriteHeader<StrategyLogRecord>();
                    csv.NextRecord();
                }

                // 写入日志记录
                var record = new StrategyLogRecord
                {
                    Timestamp = timestamp,
                    StrategyId = strategyId,
                    Message = message
                };

                csv.WriteRecord(record);
                csv.NextRecord();

                await writer.FlushAsync();
            }
        }

        public async Task<List<Tuple<DateTime, string>>> GetStrategyLogsAsync(string strategyId, DateTime startTime, DateTime endTime)
        {
            var result = new List<Tuple<DateTime, string>>();

            // 获取日期范围内的所有文件
            var startDate = startTime.Date;
            var endDate = endTime.Date;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var filePath = Path.Combine(_baseDirectory, "Strategies", $"strategy_log_{date.ToString("yyyyMMdd")}.csv");

                if (!File.Exists(filePath))
                    continue;

                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var logs = await Task.Run(() => csv.GetRecords<StrategyLogRecord>()
                        .Where(r => r.StrategyId == strategyId && r.Timestamp >= startTime && r.Timestamp <= endTime)
                        .ToList());

                    result.AddRange(logs.Select(log => new Tuple<DateTime, string>(log.Timestamp, log.Message)));
                }
            }

            // 按时间排序
            return result.OrderBy(t => t.Item1).ToList();
        }

        #region 记录

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