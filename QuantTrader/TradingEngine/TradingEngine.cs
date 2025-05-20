using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.Strategies;

namespace QuantTrader.TradingEngine
{
    public class TradingEngine : ITradingEngine
    {
        private readonly IBrokerService _brokerService;
        private readonly IMarketDataService _marketDataService;
        private readonly IDataRepository _dataRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IStrategy> _strategies = new List<IStrategy>();
        private bool _isRunning;

        public IReadOnlyList<IStrategy> Strategies => _strategies.AsReadOnly();
        public Account Account { get; private set; }
        public IBrokerService BrokerService => _brokerService;

        public event Action<string, Signal> SignalGenerated;
        public event Action<string, Order> OrderExecuted;
        public event Action<string, string> StrategyLogGenerated;
        public event Action<Account> AccountUpdated;

        public TradingEngine(
            IBrokerService brokerService,
            IMarketDataService marketDataService,
            IDataRepository dataRepository,
            IServiceProvider serviceProvider)
        {
            _brokerService = brokerService;
            _marketDataService = marketDataService;
            _dataRepository = dataRepository;
            _serviceProvider = serviceProvider;

            // 订阅券商服务事件
            _brokerService.AccountUpdated += OnAccountUpdated;
        }

        public async Task StartAsync()
        {
            if (_isRunning)
                return;

            // 连接到券商服务
            if (!_brokerService.IsConnected)
            {
                await _brokerService.ConnectAsync("demo", "password", "localhost:8888");
            }

            // 获取账户信息
            Account = await _brokerService.GetAccountInfoAsync();

            _isRunning = true;

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", "Trading engine started", DateTime.Now);
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
                return;

            // 停止所有策略
            foreach (var strategy in _strategies.ToList())
            {
                await strategy.StopAsync();
            }

            // 断开券商服务
            await _brokerService.DisconnectAsync();

            _isRunning = false;

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", "Trading engine stopped", DateTime.Now);
        }

        public async Task<IStrategy> AddStrategyAsync(string strategyType, Dictionary<string, object> parameters = null)
        {
            if (!_isRunning)
                throw new InvalidOperationException("Trading engine is not running.");

            // 生成唯一策略ID
            string strategyId = $"{strategyType}_{Guid.NewGuid():N}";

            // 创建策略实例
            IStrategy strategy = strategyType.ToLower() switch
            {
                "movingaveragecross" => new MovingAverageCrossStrategy(strategyId, _brokerService, _marketDataService, _dataRepository),
                "rsi" => new RSIStrategy(strategyId, _brokerService, _marketDataService, _dataRepository),
                "BollingerBands" => new BollingerBandsStrategy(strategyId, _brokerService, _marketDataService, _dataRepository),
                "MACD" => new MACDStrategy(strategyId, _brokerService, _marketDataService, _dataRepository),
                // 可以在这里添加其他策略类型
                _ => throw new ArgumentException($"Unsupported strategy type: {strategyType}")
            };

            // 订阅策略事件
            strategy.SignalGenerated += OnStrategySignalGenerated;
            strategy.OrderExecuted += OnStrategyOrderExecuted;
            strategy.LogGenerated += OnStrategyLogGenerated;

            // 初始化策略
            await strategy.InitializeAsync();

            // 设置策略参数
            if (parameters != null)
            {
                await strategy.UpdateParametersAsync(parameters);
            }

            // 添加到策略列表
            _strategies.Add(strategy);

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", $"Strategy added: {strategy.Name} ({strategyId})", DateTime.Now);

            return strategy;
        }

        public async Task RemoveStrategyAsync(string strategyId)
        {
            var strategy = GetStrategyOrThrow(strategyId);

            // 停止策略
            await strategy.StopAsync();

            // 取消订阅事件
            strategy.SignalGenerated -= OnStrategySignalGenerated;
            strategy.OrderExecuted -= OnStrategyOrderExecuted;
            strategy.LogGenerated -= OnStrategyLogGenerated;

            // 从列表中移除
            _strategies.Remove(strategy);

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", $"Strategy removed: {strategy.Name} ({strategyId})", DateTime.Now);
        }

        public IStrategy GetStrategy(string strategyId)
        {
            return _strategies.FirstOrDefault(s => s.Id == strategyId);
        }

        public async Task StartStrategyAsync(string strategyId)
        {
            var strategy = GetStrategyOrThrow(strategyId);

            // 启动策略
            await strategy.StartAsync();

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", $"Strategy started: {strategy.Name} ({strategyId})", DateTime.Now);
        }

        public async Task StopStrategyAsync(string strategyId)
        {
            var strategy = GetStrategyOrThrow(strategyId);

            // 停止策略
            await strategy.StopAsync();

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync("Engine", $"Strategy stopped: {strategy.Name} ({strategyId})", DateTime.Now);
        }

        public async Task UpdateStrategyParametersAsync(string strategyId, Dictionary<string, object> parameters)
        {
            var strategy = GetStrategyOrThrow(strategyId);

            // 更新策略参数
            await strategy.UpdateParametersAsync(parameters);

            // 记录日志
            await _dataRepository.LogStrategyExecutionAsync(
                "Engine",
                $"Strategy parameters updated: {strategy.Name} ({strategyId}) - {string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}"))}",
                DateTime.Now);
        }

        private void OnStrategySignalGenerated(string strategyId, Signal signal)
        {
            // 转发事件
            SignalGenerated?.Invoke(strategyId, signal);
        }

        private void OnStrategyOrderExecuted(string strategyId, Order order)
        {
            // 转发事件
            OrderExecuted?.Invoke(strategyId, order);
        }

        private void OnStrategyLogGenerated(string strategyId, string message)
        {
            // 转发事件
            StrategyLogGenerated?.Invoke(strategyId, message);
        }

        private void OnAccountUpdated(Account account)
        {
            // 更新账户
            Account = account;

            // 记录账户快照
            _dataRepository.SaveAccountSnapshotAsync(account, DateTime.Now).ConfigureAwait(false);

            // 转发事件
            AccountUpdated?.Invoke(account);
        }

        private IStrategy GetStrategyOrThrow(string strategyId)
        {
            var strategy = GetStrategy(strategyId);
            if (strategy == null)
                throw new ArgumentException($"Strategy not found: {strategyId}");

            return strategy;
        }
    }
}
