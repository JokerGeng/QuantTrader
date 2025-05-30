using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Commands;
using QuantTrader.Models;
using QuantTrader.Strategies;
using QuantTrader.TradingEngines;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.Views;
using System.Reflection;

namespace QuantTrader.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IServiceProvider _serviceProvider;

        private bool _isEngineRunning;
        private AccountViewModel _account;
        private StrategyBase _selectedStrategy;
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

        public ObservableCollection<StrategyBase> Strategies { get; } = new ObservableCollection<StrategyBase>();

        public StrategyBase SelectedStrategy
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
        public ICommand ReconnectBrokerCommand { get; }
        public ICommand ConfigureStrategyCommand { get; }
        public ICommand CreateCustomStrategyCommand { get; }
        public ICommand OpenStockManagerCommand { get; }

        public MainViewModel(IServiceProvider serviceProvider ,ITradingEngine tradingEngine)
        {
            _serviceProvider = serviceProvider;
            _tradingEngine = tradingEngine;

            if (_tradingEngine is TradingEngine engine &&
      engine.BrokerService != null)
            {
                var brokerService = engine.BrokerService;
                IsBrokerConnected = brokerService.IsConnected;
                BrokerConnectionInfo = brokerService.ConnectionInfo?.ToString() ?? "Not connected";

                // 订阅连接状态变更事件
                brokerService.ConnectionStatusChanged += OnBrokerConnectionStatusChanged;
            }

            // 初始化命令
            StartEngineCommand = new AsyncRelayCommand(ExecuteStartEngineAsync, ()=> !IsEngineRunning);
            StopEngineCommand = new AsyncRelayCommand(ExecuteStopEngineAsync, () => IsEngineRunning);
            //AddStrategyCommand = new AsyncRelayCommand(ExecuteAddStrategyAsync, () => IsEngineRunning);
            //RemoveStrategyCommand = new AsyncRelayCommand(ExecuteRemoveStrategyAsync, () => SelectedStrategy != null);
            //StartStrategyCommand = new AsyncRelayCommand(ExecuteStartStrategyAsync, () => SelectedStrategy != null && SelectedStrategy.Status != StrategyStatus.Running);
            //StopStrategyCommand = new AsyncRelayCommand(ExecuteStopStrategyAsync, () => SelectedStrategy != null && SelectedStrategy.Status == StrategyStatus.Running);
            ConfigureStrategyCommand = new AsyncRelayCommand(ExecuteConfigureStrategyAsync);
            ReconnectBrokerCommand = new RelayCommand(ExecuteReconnectBroker, () => !IsBrokerConnected);
            CreateCustomStrategyCommand = new RelayCommand(ExecuteCreateCustomStrategy, () => IsEngineRunning);
            OpenStockManagerCommand = new RelayCommand(ExecuteOpenStockManager, () => IsEngineRunning);
            //OpenStrategyManagerCommand = new RelayCommand( ExecuteOpenStrategyManager, () => IsEngineRunning);
            // 订阅交易引擎事件
            _tradingEngine.SignalGenerated += OnSignalGenerated;
            _tradingEngine.OrderExecuted += OnOrderExecuted;
            _tradingEngine.StrategyLogGenerated += OnStrategyLogGenerated;
            _tradingEngine.AccountUpdated += OnAccountUpdated;
            LoadDefaultStrategies();
        }

        void LoadDefaultStrategies()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var engine = _tradingEngine as TradingEngine;
            var types = assembly.GetTypes().Where(t => t.Namespace == "QuantTrader.Strategies");
            foreach (var type in types)
            {
                if(type.IsSubclassOf(typeof(StrategyBase)) && !type.IsAbstract)
                {
                    string strategyId = $"_{Guid.NewGuid():N}";
                    var strategy = (StrategyBase)Activator.CreateInstance
                        (type, strategyId, engine.BrokerService, engine.MarketDataService, engine.DataRepository);
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

        private async Task ExecuteAddStrategyAsync()
        {
            try
            {
                // 显示策略配置对话框
                var configWindow = new StrategyConfigWindow();
                var result = configWindow.ShowDialog();

                if (result != true)
                    return;

                //// 获取参数
                //var strategyType = configWindow.ViewModel.StrategyType;
                //var parameters = configWindow.Parameters;

                //// 添加策略
                //var strategy = await _tradingEngine.AddStrategyAsync(strategyType, parameters);

                //ExecuteOnUI(() =>
                //{
                //    var strategyViewModel = new StrategyViewModel(strategy);
                //    Strategies.Add(strategyViewModel);
                //    SelectedStrategy = strategyViewModel;
                //});

                //StatusMessage = $"Strategy '{strategy.Name}' added successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding strategy: {ex.Message}";
            }
        }

        private async Task ExecuteRemoveStrategyAsync()
        {
            if (SelectedStrategy == null)
                return;

            try
            {
                StatusMessage = $"Removing strategy '{SelectedStrategy.Name}'...";
                await _tradingEngine.RemoveStrategyAsync(SelectedStrategy.Id);

                ExecuteOnUI(() =>
                {
                    Strategies.Remove(SelectedStrategy);
                    SelectedStrategy = null;
                });

                StatusMessage = "Strategy removed successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error removing strategy: {ex.Message}";
            }
        }

        private async Task ExecuteStartStrategyAsync()
        {
            if (SelectedStrategy == null)
                return;

            try
            {
                StatusMessage = $"Starting strategy '{SelectedStrategy.Name}'...";
                await _tradingEngine.StartStrategyAsync(SelectedStrategy.Id);

                // 更新视图模型
                //SelectedStrategy.Status = StrategyStatus.Running;

                StatusMessage = $"Strategy '{SelectedStrategy.Name}' started successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting strategy: {ex.Message}";
            }
        }

        private async Task ExecuteStopStrategyAsync()
        {
            if (SelectedStrategy == null)
                return;

            try
            {
                StatusMessage = $"Stopping strategy '{SelectedStrategy.Name}'...";
                await _tradingEngine.StopStrategyAsync(SelectedStrategy.Id);

                // 更新视图模型
               // SelectedStrategy.Status = StrategyStatus.Stopped;

                StatusMessage = $"Strategy '{SelectedStrategy.Name}' stopped successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping strategy: {ex.Message}";
            }
        }

        private async Task ExecuteConfigureStrategyAsync()
        {
            if (SelectedStrategy == null)
                return;

            try
            {
                // 显示策略配置对话框
                var configWindow = new StrategyConfigWindow();
               configWindow.ViewModel.Strategy= SelectedStrategy;

                var result = configWindow.ShowDialog();
                if (result != true)
                    return;

                //// 获取参数
                //var parameters = configWindow.Parameters;

                //// 更新策略参数
                //await _tradingEngine.UpdateStrategyParametersAsync(SelectedStrategy.Id, parameters);

                //// 更新视图模型
                //foreach (var param in parameters)
                //{
                //    SelectedStrategy.Parameters[param.Key] = param.Value;
                //}

                StatusMessage = $"Strategy '{SelectedStrategy.Name}' configured successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error configuring strategy: {ex.Message}";
            }
        }

        private void ExecuteOpenStrategyManager()
        {
            try
            {
                
            }
            catch (Exception ex)
            {
                StatusMessage = $"打开Strategy管理窗口失败: {ex.Message}";
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

        private void ExecuteReconnectBroker()
        {
            try
            {
                StatusMessage = "Attempting to reconnect to broker...";

                // 显示登录窗口重新连接
                var loginWindow = new LoginWindow(new LoginViewModel(new BrokerServiceFactory(_serviceProvider),new MarketDataServiceFactory(_serviceProvider)));
                var result = loginWindow.ShowDialog();

                if (result == true && loginWindow.BrokerService != null)
                {
                    // 重新连接成功，更新交易引擎的券商服务
                    // 注意：这需要交易引擎支持热替换券商服务
                    StatusMessage = "Reconnected to broker successfully.";
                }
                else
                {
                    StatusMessage = "Reconnection cancelled or failed.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Reconnection error: {ex.Message}";
            }
        }

        private  void ExecuteCreateCustomStrategy()
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
