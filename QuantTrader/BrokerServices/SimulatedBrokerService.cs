using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using System.Timers;

namespace QuantTrader.BrokerServices
{
    public class SimulatedBrokerService : IBrokerService, IDisposable
    {
        private readonly IMarketDataService _marketDataService;
        private readonly Random _random = new Random();
        private readonly System.Timers.Timer _simulationTimer;
        private readonly Dictionary<string, Order> _orders = new Dictionary<string, Order>();
        private Account _account;
        private bool _connected;

        public event Action<Order> OrderStatusChanged;
        public event Action<Order> OrderExecuted;
        public event Action<Account> AccountUpdated;

        public bool IsConnected => _connected;

        public SimulatedBrokerService(IMarketDataService marketDataService)
        {
            _marketDataService = marketDataService;
            _simulationTimer = new System.Timers.Timer(500); // 每500毫秒模拟一次订单执行
            _simulationTimer.Elapsed += OnSimulationTimerElapsed;

            // 初始化模拟账户
            _account = new Account("SIM001", 1000000);
        }

        public Task<bool> ConnectAsync(string username, string password, string serverAddress)
        {
            _connected = true;
            _simulationTimer.Start();

            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            _connected = false;
            _simulationTimer.Stop();

            return Task.CompletedTask;
        }

        public Task<Account> GetAccountInfoAsync()
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            return Task.FromResult(_account);
        }

        public Task<List<Position>> GetPositionsAsync()
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            return Task.FromResult(_account.Positions.Values.ToList());
        }

        public async Task<Order> PlaceOrderAsync(string symbol, OrderDirection direction, OrderType type, decimal price, int quantity, string strategyId)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Symbol cannot be empty.", nameof(symbol));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            if (type == OrderType.Limit && price <= 0)
                throw new ArgumentException("Limit price must be positive.", nameof(price));

            // 检查行情数据是否存在
            var marketData = await _marketDataService.GetLevel1DataAsync(symbol);
            if (marketData == null)
                throw new ArgumentException($"No market data available for symbol: {symbol}");

            // 创建订单
            var orderId = Guid.NewGuid().ToString("N");
            var order = new Order
            {
                OrderId = orderId,
                Symbol = symbol,
                Direction = direction,
                Type = type,
                Price = type == OrderType.Market ? marketData.LastPrice : price,
                StopPrice = 0,
                Quantity = quantity,
                FilledQuantity = 0,
                Status = OrderStatus.Created,
                CreateTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                StrategyId = strategyId ?? "Manual",
                Message = "Order created"
            };

            // 添加到订单列表
            _orders[orderId] = order;

            // 更改订单状态为已提交
            await Task.Delay(50); // 模拟网络延迟

            order.Status = OrderStatus.Submitted;
            order.UpdateTime = DateTime.Now;
            order.Message = "Order submitted";

            // 触发订单状态变更事件
            OrderStatusChanged?.Invoke(order);

            return order;
        }

        public async Task<bool> CancelOrderAsync(string orderId)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            if (!_orders.TryGetValue(orderId, out var order))
                throw new ArgumentException($"Order not found: {orderId}");

            if (!order.IsActive)
                return false;

            // 模拟网络延迟
            await Task.Delay(50);

            // 有1/10的概率取消失败
            if (_random.Next(10) == 0)
                return false;

            // 更新订单状态
            order.Status = OrderStatus.Canceled;
            order.UpdateTime = DateTime.Now;
            order.Message = "Order canceled";

            // 触发订单状态变更事件
            OrderStatusChanged?.Invoke(order);

            return true;
        }

        public Task<Order> GetOrderAsync(string orderId)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            if (!_orders.TryGetValue(orderId, out var order))
                throw new ArgumentException($"Order not found: {orderId}");

            return Task.FromResult(order);
        }

        public Task<List<Order>> GetOrdersAsync(string symbol = null, OrderStatus? status = null)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to broker.");

            var query = _orders.Values.AsQueryable();

            if (!string.IsNullOrEmpty(symbol))
                query = query.Where(o => o.Symbol == symbol);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            return Task.FromResult(query.ToList());
        }

        private async void OnSimulationTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_connected)
                return;

            // 处理活跃订单
            var activeOrders = _orders.Values
                .Where(o => o.IsActive)
                .ToList();

            foreach (var order in activeOrders)
            {
                // 获取最新行情数据
                var marketData = await _marketDataService.GetLevel1DataAsync(order.Symbol);
                if (marketData == null)
                    continue;

                // 检查是否可以成交
                bool canExecute = false;
                decimal executionPrice = 0;

                switch (order.Type)
                {
                    case OrderType.Market:
                        canExecute = true;
                        executionPrice = order.Direction == OrderDirection.Buy ?
                            marketData.AskPrice1 : marketData.BidPrice1;
                        break;
                    case OrderType.Limit:
                        if (order.Direction == OrderDirection.Buy)
                        {
                            canExecute = marketData.AskPrice1 <= order.Price;
                            executionPrice = Math.Min(order.Price, marketData.AskPrice1);
                        }
                        else
                        {
                            canExecute = marketData.BidPrice1 >= order.Price;
                            executionPrice = Math.Max(order.Price, marketData.BidPrice1);
                        }
                        break;
                }

                if (canExecute)
                {
                    // 随机决定成交数量
                    var remainingQuantity = order.Quantity - order.FilledQuantity;

                    // 有1/3的概率部分成交，2/3的概率全部成交
                    int executionQuantity;
                    if (_random.Next(3) == 0 && remainingQuantity > 1)
                    {
                        executionQuantity = _random.Next(1, remainingQuantity);
                    }
                    else
                    {
                        executionQuantity = remainingQuantity;
                    }

                    // 更新订单信息
                    order.FilledQuantity += executionQuantity;
                    order.UpdateTime = DateTime.Now;

                    // 计算平均成交价
                    if (order.AverageFilledPrice == 0)
                    {
                        order.AverageFilledPrice = executionPrice;
                    }
                    else
                    {
                        var totalValue = order.AverageFilledPrice * (order.FilledQuantity - executionQuantity) +
                                        executionPrice * executionQuantity;
                        order.AverageFilledPrice = totalValue / order.FilledQuantity;
                    }

                    // 更新订单状态
                    if (order.FilledQuantity >= order.Quantity)
                    {
                        order.Status = OrderStatus.Filled;
                        order.Message = "Order fully filled";
                    }
                    else
                    {
                        order.Status = OrderStatus.PartiallyFilled;
                        order.Message = $"Order partially filled: {order.FilledQuantity}/{order.Quantity}";
                    }

                    // 触发订单执行事件
                    OrderExecuted?.Invoke(order);

                    // 触发订单状态变更事件
                    OrderStatusChanged?.Invoke(order);

                    // 更新持仓
                    UpdatePositionForOrder(order, executionQuantity, executionPrice);
                }
            }
        }

        private void UpdatePositionForOrder(Order order, int executionQuantity, decimal executionPrice)
        {
            // 获取或创建持仓
            if (!_account.Positions.TryGetValue(order.Symbol, out var position))
            {
                position = new Position(order.Symbol);
                _account.Positions[order.Symbol] = position;
            }

            // 更新持仓
            int positionChange = order.Direction == OrderDirection.Buy ? executionQuantity : -executionQuantity;
            decimal positionCost = executionPrice * Math.Abs(positionChange);

            // 如果是建仓或加仓
            if ((position.Quantity >= 0 && positionChange > 0) ||
                (position.Quantity <= 0 && positionChange < 0))
            {
                // 计算新的平均成本
                decimal totalCost = position.AverageCost * Math.Abs(position.Quantity) + positionCost;
                int totalQuantity = Math.Abs(position.Quantity) + Math.Abs(positionChange);
                position.AverageCost = totalCost / totalQuantity;
            }
            // 如果是减仓或平仓
            else if ((position.Quantity > 0 && positionChange < 0) ||
                    (position.Quantity < 0 && positionChange > 0))
            {
                // 如果完全平仓或方向反转
                if (Math.Abs(positionChange) >= Math.Abs(position.Quantity))
                {
                    // 计算平仓盈亏
                    decimal closingPnL = 0;
                    if (position.Quantity > 0) // 多仓平仓
                    {
                        closingPnL = (executionPrice - position.AverageCost) * position.Quantity;
                    }
                    else // 空仓平仓
                    {
                        closingPnL = (position.AverageCost - executionPrice) * Math.Abs(position.Quantity);
                    }

                    // 更新账户余额
                    _account.Cash += closingPnL;

                    // 如果有反转，设置新的平均成本
                    int remainingQuantity = Math.Abs(positionChange) - Math.Abs(position.Quantity);
                    if (remainingQuantity > 0)
                    {
                        position.AverageCost = executionPrice;
                    }
                    else
                    {
                        position.AverageCost = 0;
                    }
                }
                // 否则保持原有平均成本
            }

            // 更新持仓数量
            position.Quantity += positionChange;

            // 更新当前价格
            position.CurrentPrice = executionPrice;

            // 更新账户现金
            _account.Cash -= positionCost * (order.Direction == OrderDirection.Buy ? 1 : -1);

            // 更新账户总资产价值
            _account.TotalAssetValue = _account.Cash + _account.Positions.Values.Sum(p => p.MarketValue);

            // 触发账户更新事件
            AccountUpdated?.Invoke(_account);
        }

        public void Dispose()
        {
            _simulationTimer?.Stop();
            _simulationTimer?.Dispose();
        }
    }
}
