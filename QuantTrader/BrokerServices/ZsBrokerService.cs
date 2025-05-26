using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.BrokerServices
{
    /// <summary>
    /// 招商证券券商服务实现
    /// </summary>
    public class ZsBrokerService : IBrokerService
    {
        // 与ThsBrokerService类似的实现结构
        public bool IsConnected { get; private set; }
        public BrokerConnectionInfo ConnectionInfo { get; private set; }

        public IMarketDataService MarketDataService => throw new NotImplementedException();

        public event Action<Order> OrderStatusChanged;
        public event Action<Order> OrderExecuted;
        public event Action<Account> AccountUpdated;
        public event Action<bool> ConnectionStatusChanged;

        public async Task<bool> ConnectAsync(string username, string password, string serverAddress)
        {
            await Task.Delay(1500);
            IsConnected = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
            return IsConnected;
        }

        public async Task DisconnectAsync() => IsConnected = false;
        public async Task<Account> GetAccountInfoAsync() => new Account("ZS_" + DateTime.Now.Ticks, 600000);
        public async Task<List<Position>> GetPositionsAsync() => new List<Position>();
        public async Task<Order> PlaceOrderAsync(string symbol, OrderDirection direction, OrderType type, decimal price, int quantity, string strategyId) => new Order { OrderId = Guid.NewGuid().ToString() };
        public async Task<bool> CancelOrderAsync(string orderId) => true;
        public async Task<Order> GetOrderAsync(string orderId) => throw new NotImplementedException();
        public async Task<List<Order>> GetOrdersAsync(string symbol = null, OrderStatus? status = null) => new List<Order>();
    }
}
