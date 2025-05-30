using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public abstract class StrategyBase : IStrategyRuntime
    {
        protected readonly IBrokerService _brokerService;
        protected readonly IMarketDataService _marketDataService;
        protected readonly IDataRepository _dataRepository;

        public IStrategyInfo StrategyInfo { get; }
        public string Symbol { get;  set; }

        public StrategyStatus Status { get; protected set; }

        public event Action<string, Order> OrderExecuted;
        public event Action<string, Signal> SignalGenerated;
        public event Action<string, string> LogGenerated;

        protected Dictionary<string, Position> Positions { get; } = new Dictionary<string, Position>();
        protected Dictionary<string, Order> ActiveOrders { get; } = new Dictionary<string, Order>();

        protected StrategyBase(
            IStrategyInfo strategyInfo,
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository)
        {
            StrategyInfo = strategyInfo;
            _brokerService = brokerService;
            _marketDataService = marketDataService;
            _dataRepository = dataRepository;
            Status = StrategyStatus.Initialized;
        }

        public virtual async Task InitializeAsync()
        {
            // 查询当前持仓
            var positions = await _brokerService.GetPositionsAsync();
            foreach (var position in positions)
            {
                Positions[position.Symbol] = position;
            }

            // 查询活跃订单
            var orders = await _brokerService.GetOrdersAsync(status: OrderStatus.Created);
            orders.AddRange(await _brokerService.GetOrdersAsync(status: OrderStatus.Submitted));
            orders.AddRange(await _brokerService.GetOrdersAsync(status: OrderStatus.PartiallyFilled));

            foreach (var order in orders)
            {
                if (order.StrategyId == StrategyInfo.Id)
                {
                    ActiveOrders[order.OrderId] = order;
                }
            }

            // 订阅订单状态变更事件
            _brokerService.OrderStatusChanged += OnOrderStatusChanged;
            _brokerService.OrderExecuted += OnOrderExecuted;

            Log("Strategy initialized");
        }

        public virtual async Task StartAsync()
        {
            if (Status == StrategyStatus.Running)
                return;

            Status = StrategyStatus.Running;
            Log("Strategy started");

            await Task.CompletedTask;
        }

        public virtual async Task StopAsync()
        {
            if (Status == StrategyStatus.Stopped)
                return;

            // 取消所有活跃订单
            foreach (var orderId in ActiveOrders.Keys.ToList())
            {
                try
                {
                    await _brokerService.CancelOrderAsync(orderId);
                }
                catch (Exception ex)
                {
                    Log($"Error canceling order {orderId}: {ex.Message}");
                }
            }

            Status = StrategyStatus.Stopped;
            Log("Strategy stopped");
        }

        protected virtual void OnOrderStatusChanged(Order order)
        {
            if (order.StrategyId != StrategyInfo.Id)
                return;

            if (order.IsActive)
            {
                ActiveOrders[order.OrderId] = order;
            }
            else
            {
                ActiveOrders.Remove(order.OrderId);
            }

            Log($"Order status changed: {order}");
        }

        protected virtual void OnOrderExecuted(Order order)
        {
            if (order.StrategyId != StrategyInfo.Id)
                return;

            Log($"Order executed: {order}");

            OrderExecuted?.Invoke(StrategyInfo.Id, order);
        }

        protected async Task<Order> PlaceOrderAsync(Signal signal)
        {
            try
            {
                var direction = signal.Type == SignalType.Buy ? OrderDirection.Buy : OrderDirection.Sell;
                var order = await _brokerService.PlaceOrderAsync(
                    signal.Symbol,
                    direction,
                    OrderType.Limit,
                    signal.Price,
                    signal.Quantity,
                    StrategyInfo.Id);

                Log($"Order placed: {order}");
                return order;
            }
            catch (Exception ex)
            {
                Log($"Error placing order: {ex.Message}");
                throw;
            }
        }

        protected void GenerateSignal(Signal signal)
        {
            Log($"Signal generated: {signal}");

            SignalGenerated?.Invoke(StrategyInfo.Id, signal);
        }

        protected void Log(string message)
        {
            var timestamp = DateTime.Now;

            // 记录到数据库
            _dataRepository.LogStrategyExecutionAsync(StrategyInfo.Id, message, timestamp).ConfigureAwait(false);

            // 触发日志事件
            LogGenerated?.Invoke(StrategyInfo.Id, message);
        }
    }
}
