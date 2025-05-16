using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Models;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 数据持久化接口
    /// </summary>
    public interface IDataRepository
    {
        // 保存订单记录
        Task SaveOrderAsync(Order order);

        // 查询订单历史
        Task<List<Order>> GetOrderHistoryAsync(DateTime startTime, DateTime endTime, string symbol = null);

        // 保存账户快照
        Task SaveAccountSnapshotAsync(Account account, DateTime timestamp);

        // 获取账户历史快照
        Task<List<Tuple<DateTime, Account>>> GetAccountHistoryAsync(DateTime startTime, DateTime endTime);

        // 保存策略执行日志
        Task LogStrategyExecutionAsync(string strategyId, string message, DateTime timestamp);

        // 获取策略执行日志
        Task<List<Tuple<DateTime, string>>> GetStrategyLogsAsync(string strategyId, DateTime startTime, DateTime endTime);
    }
}
