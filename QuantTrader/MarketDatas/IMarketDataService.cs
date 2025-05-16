using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 行情数据服务接口
    /// </summary>
    public interface IMarketDataService
    {
        /// <summary>
        /// 获取实时行情数据
        /// </summary>
        Task<Level1Data> GetLevel1DataAsync(string symbol);

        /// <summary>
        /// 订阅实时行情数据
        /// </summary>
        void SubscribeLevel1Data(string symbol, Action<Level1Data> callback);

        /// <summary>
        /// 取消订阅实时行情数据
        /// </summary>
        void UnsubscribeLevel1Data(string symbol, Action<Level1Data> callback);

        /// <summary>
        /// 获取历史K线数据
        /// </summary>
        Task<List<Candlestick>> GetHistoricalCandlesticksAsync(
            string symbol,
            DateTime startTime,
            DateTime endTime,
            TimeSpan period);

        /// <summary>
        /// 获取最新的N根K线
        /// </summary>
        Task<List<Candlestick>> GetLatestCandlesticksAsync(
            string symbol,
            int count,
            TimeSpan period);
    }
}
