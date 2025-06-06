﻿using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuantTrader.BrokerServices;
using QuantTrader.Commands;
using QuantTrader.MarketDatas;
using QuantTrader.Models;
using QuantTrader.Strategies;
using QuantTrader.TradingEngines;
using QuantTrader.Views;

namespace QuantTrader.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IServiceProvider _serviceProvider;

        private bool _isEngineRunning;
        private AccountViewModel _account;
        private IStrategy _selectedStrategy;
        private string _statusMessage;
        private bool _isBrokerConnected;
        private string _brokerConnectionInfo;

        public bool IsBrokerConnected
        {
            get => _isBrokerConnected;
            private set => SetProperty(ref _isBrokerConnected, value);
        }

        public string BrokerConnectionInfo
        {
            get => _brokerConnectionInfo;
            private set => SetProperty(ref _brokerConnectionInfo, value);
        }

        public bool IsEngineRunning
        {
            get => _isEngineRunning;
            private set => SetProperty(ref _isEngineRunning, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public AccountViewModel Account
        {
            get => _account;
            private set => SetProperty(ref _account, value);
        }

        public ObservableCollection<IStrategy> Strategies { get; } = new ObservableCollection<IStrategy>();

        public IStrategy SelectedStrategy
        {
            get => _selectedStrategy;
            set => SetProperty(ref _selectedStrategy, value);
        }

        public ObservableCollection<OrderViewModel> Orders { get; } = new ObservableCollection<OrderViewModel>();

        public ObservableCollection<SignalViewModel> Signals { get; } = new ObservableCollection<SignalViewModel>();

        public ObservableCollection<PositionViewModel> Positions { get; } = new ObservableCollection<PositionViewModel>();

        public ObservableCollection<LogEntryViewModel> LogEntries { get; } = new ObservableCollection<LogEntryViewModel>();

        public ICommand StartEngineCommand { get; }
        public ICommand StopEngineCommand { get; }
        public ICommand ConfigureStrategyCommand { get; }
        public ICommand CreateCustomStrategyCommand { get; }
        public ICommand OpenStockManagerCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider, ITradingEngine tradingEngine)
        {
            _serviceProvider = serviceProvider;
            _tradingEngine = tradingEngine;

            if (_tradingEngine is TradingEngine engine && engine.BrokerService != null)
            {
                var brokerService = engine.BrokerService;
                IsBrokerConnected = brokerService.IsConnected;
                BrokerConnectionInfo = brokerService.ConnectionInfo?.ToString() ?? "Not connected";

                // 订阅连接状态变更事件
                brokerService.ConnectionStatusChanged += OnBrokerConnectionStatusChanged;
            }

            // 初始化命令
            StartEngineCommand = new AsyncRelayCommand(ExecuteStartEngineAsync, () => !IsEngineRunning);
            StopEngineCommand = new AsyncRelayCommand(ExecuteStopEngineAsync, () => IsEngineRunning);
            ConfigureStrategyCommand = new RelayCommand(ExecuteConfigureStrategy);
            CreateCustomStrategyCommand = new RelayCommand(ExecuteCreateCustomStrategy);
            OpenStockManagerCommand = new RelayCommand(ExecuteOpenStockManager, () => IsEngineRunning);

            // 订阅交易引擎事件
            _tradingEngine.SignalGenerated += OnSignalGenerated;
            _tradingEngine.OrderExecuted += OnOrderExecuted;
            _tradingEngine.StrategyLogGenerated += OnStrategyLogGenerated;
            _tradingEngine.AccountUpdated += OnAccountUpdated;

        }

        void LoadDefaultStrategies()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes().Where(t => t.Namespace == "QuantTrader.Strategies");
            foreach (var type in types)
            {
                if (type.IsSubclassOf(typeof(StrategyBase)) && !type.IsAbstract)
                {
                    var strategy = (StrategyBase)Activator.CreateInstance(type);
                    if (strategy != null)
                    {
                        this.Strategies.Add(strategy);
                    }
                }
            }
        }

        private async Task ExecuteStartEngineAsync()
        {
            try
            {
                StatusMessage = "Starting trading engine...";
                await _tradingEngine.StartAsync();

                UpdateAccount(_tradingEngine.Account);
                IsEngineRunning = true;

                StatusMessage = "Trading engine started successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting trading engine: {ex.Message}";
            }
        }

        private async Task ExecuteStopEngineAsync()
        {
            try
            {
                StatusMessage = "Stopping trading engine...";
                await _tradingEngine.StopAsync();

                IsEngineRunning = false;
                StatusMessage = "Trading engine stopped.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping trading engine: {ex.Message}";
            }
        }

        private void ExecuteConfigureStrategy()
        {
            if (SelectedStrategy == null)
                return;

            try
            {
                // 显示策略配置对话框
                var configWindow = new StrategyConfigWindow();
                configWindow.ViewModel.Strategy = SelectedStrategy;

                var result = configWindow.ShowDialog();
                if (result != true)
                    return;

                StatusMessage = $"Strategy '{SelectedStrategy.Name}' configured successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error configuring strategy: {ex.Message}";
            }
        }

        private void ExecuteCreateCustomStrategy()
        {
            try
            {
                // 创建脚本编辑器视图模型
                var scriptEditorViewModel = new ScriptEditorViewModel(_tradingEngine);

                // 显示脚本编辑器窗口
                var scriptEditorWindow = new ScriptEditorWindow(scriptEditorViewModel);
                var result = scriptEditorWindow.ShowDialog();

                if (result == true && scriptEditorWindow.ScriptStrategy != null)
                {
                    // 添加策略到视图模型列表
                    var strategy = scriptEditorWindow.ScriptStrategy;

                    //ExecuteOnUI(() =>
                    //{
                    //    var strategyViewModel = new StrategyViewModel(strategy);
                    //    Strategies.Add(strategyViewModel);
                    //    SelectedStrategy = strategyViewModel;
                    //});

                    StatusMessage = $"Script strategy '{strategy.Name}' created successfully.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating script strategy: {ex.Message}";
            }
        }

        private void ExecuteOpenStockManager()
        {
            try
            {
                var stockManagerViewModel = new StockManagerViewModel(
                    _serviceProvider.GetRequiredService<IMarketDataService>(),
                    _tradingEngine);

                var stockManagerWindow = new StockManagerWindow(stockManagerViewModel);
                stockManagerWindow.Show();
            }
            catch (Exception ex)
            {
                StatusMessage = $"打开股票管理窗口失败: {ex.Message}";
            }
        }

        private void OnSignalGenerated(string strategyId, Signal signal)
        {
            ExecuteOnUI(() =>
            {
                var signalViewModel = new SignalViewModel
                {
                    StrategyId = strategyId,
                    Symbol = signal.Symbol,
                    Type = signal.Type.ToString(),
                    Price = signal.Price,
                    Quantity = signal.Quantity,
                    Timestamp = signal.Timestamp,
                    Reason = signal.Reason
                };

                Signals.Insert(0, signalViewModel);

                // 限制显示条数
                while (Signals.Count > 100)
                {
                    Signals.RemoveAt(Signals.Count - 1);
                }
            });
        }

        private void OnOrderExecuted(string strategyId, Order order)
        {
            ExecuteOnUI(() =>
            {
                // 添加或更新订单
                var existingOrder = Orders.FirstOrDefault(o => o.OrderId == order.OrderId);
                if (existingOrder != null)
                {
                    existingOrder.Status = order.Status.ToString();
                    existingOrder.FilledQuantity = order.FilledQuantity;
                    existingOrder.AverageFilledPrice = order.AverageFilledPrice;
                    existingOrder.UpdateTime = order.UpdateTime;
                }
                else
                {
                    var orderViewModel = new OrderViewModel
                    {
                        OrderId = order.OrderId,
                        StrategyId = strategyId,
                        Symbol = order.Symbol,
                        Direction = order.Direction.ToString(),
                        Type = order.Type.ToString(),
                        Price = order.Price,
                        Quantity = order.Quantity,
                        FilledQuantity = order.FilledQuantity,
                        Status = order.Status.ToString(),
                        CreateTime = order.CreateTime,
                        UpdateTime = order.UpdateTime,
                        AverageFilledPrice = order.AverageFilledPrice
                    };

                    Orders.Insert(0, orderViewModel);
                }

                // 限制显示条数
                while (Orders.Count > 100)
                {
                    Orders.RemoveAt(Orders.Count - 1);
                }
            });
        }

        private void OnStrategyLogGenerated(string strategyId, string message)
        {
            ExecuteOnUI(() =>
            {
                var logEntry = new LogEntryViewModel
                {
                    Timestamp = DateTime.Now,
                    StrategyId = strategyId,
                    Message = message
                };

                LogEntries.Insert(0, logEntry);

                // 限制显示条数
                while (LogEntries.Count > 1000)
                {
                    LogEntries.RemoveAt(LogEntries.Count - 1);
                }
            });
        }

        private void OnAccountUpdated(Account account)
        {
            ExecuteOnUI(() => UpdateAccount(account));
        }

        private void OnBrokerConnectionStatusChanged(bool isConnected)
        {
            ExecuteOnUI(() =>
            {
                IsBrokerConnected = isConnected;

                if (isConnected && _tradingEngine is TradingEngine engine)
                {
                    BrokerConnectionInfo = engine.BrokerService.ConnectionInfo?.ToString() ?? "Connected";
                    StatusMessage = "Broker connected successfully.";
                }
                else
                {
                    BrokerConnectionInfo = "Not connected";
                    StatusMessage = "Broker disconnected.";
                }
            });
        }

        private void UpdateAccount(Account account)
        {
            if (account == null)
                return;

            Account = new AccountViewModel
            {
                AccountId = account.AccountId,
                Cash = account.Cash,
                TotalAssetValue = account.TotalAssetValue,
                MarginUsed = account.MarginUsed,
                MarginAvailable = account.MarginAvailable
            };

            // 更新持仓列表
            Positions.Clear();
            foreach (var position in account.Positions.Values)
            {
                Positions.Add(new PositionViewModel
                {
                    Symbol = position.Symbol,
                    Quantity = position.Quantity,
                    AverageCost = position.AverageCost,
                    CurrentPrice = position.CurrentPrice,
                    MarketValue = position.MarketValue,
                    UnrealizedPnL = position.UnrealizedPnL,
                    UnrealizedPnLPercent = position.UnrealizedPnLPercent
                });
            }
        }
    }
}
