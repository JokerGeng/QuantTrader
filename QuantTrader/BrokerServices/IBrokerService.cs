using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Models;

namespace QuantTrader.BrokerServices
{
    /// <summary>
    /// 券商服务接口
    /// </summary>
    public interface IBrokerService
    {
        /// <summary>
        /// 连接到交易服务器
        /// </summary>
        Task<bool> ConnectAsync(string username, string password, string serverAddress);

        /// <summary>
        /// 断开连接
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// 获取连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 获取账户信息
        /// </summary>
        Task<Account> GetAccountInfoAsync();

        /// <summary>
        /// 获取持仓
        /// </summary>
        Task<List<Position>> GetPositionsAsync();

        /// <summary>
        /// 下单
        /// </summary>
        Task<Order> PlaceOrderAsync(string symbol, OrderDirection direction, OrderType type, decimal price, int quantity, string strategyId);

        /// <summary>
        /// 取消订单
        /// </summary>
        Task<bool> CancelOrderAsync(string orderId);

        /// <summary>
        /// 查询订单状态
        /// </summary>
        Task<Order> GetOrderAsync(string orderId);

        /// <summary>
        /// 查询订单列表
        /// </summary>
        Task<List<Order>> GetOrdersAsync(string symbol = null, OrderStatus? status = null);

        /// <summary>
        /// 订单状态变更事件
        /// </summary>
        event Action<Order> OrderStatusChanged;

        /// <summary>
        /// 成交回报事件
        /// </summary>
        event Action<Order> OrderExecuted;

        /// <summary>
        /// 账户更新事件
        /// </summary>
        event Action<Account> AccountUpdated;
    }
}
