using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using QuantTrader.BrokerServices;
using QuantTrader.Commands;
using QuantTrader.MarketDatas;
using QuantTrader.Models;

namespace QuantTrader.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly BrokerServiceFactory _brokerServiceFactory;
        private readonly MarketDataServiceFactory _marketDataServiceFactory;

        private LoginMode _selectedMode;
        private string _selectedBrokerType;
        private string _brokerServerAddress;
        private string _brokerUsername;
        private string _brokerPassword;
        private string _selectedMarketDataSource;
        private string _marketDataServerAddress;
        private string _marketDataUsername;
        private string _marketDataPassword;
        private string _marketDataApiKey;
        private bool _isLogging;
        private string _statusMessage;
        private bool _rememberCredentials;

        public LoginMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (SetProperty(ref _selectedMode, value))
                {
                    OnModeChanged();
                    OnPropertyChanged(nameof(CanLogin));
                    OnPropertyChanged(nameof(IsMarketDataConfigVisible));
                    OnPropertyChanged(nameof(ModeDescription));
                }
            }
        }

        public string SelectedBrokerType
        {
            get => _selectedBrokerType;
            set
            {
                if (SetProperty(ref _selectedBrokerType, value))
                {
                    LoadDefaultBrokerSettings();
                    OnPropertyChanged(nameof(CanLogin));
                }
            }
        }

        public string BrokerServerAddress
        {
            get => _brokerServerAddress;
            set
            {
                SetProperty(ref _brokerServerAddress, value);
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        [Required(ErrorMessage = "券商用户名必填")]
        public string BrokerUsername
        {
            get => _brokerUsername;
            set
            {
                SetProperty(ref _brokerUsername, value);
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        [Required(ErrorMessage = "券商密码必填")]
        public string BrokerPassword
        {
            get => _brokerPassword;
            set
            {
                SetProperty(ref _brokerPassword, value);
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        public string SelectedMarketDataSource
        {
            get => _selectedMarketDataSource;
            set
            {
                if (SetProperty(ref _selectedMarketDataSource, value))
                {
                    LoadDefaultMarketDataSettings();
                    OnPropertyChanged(nameof(CanLogin));
                    OnPropertyChanged(nameof(IsApiKeyVisible));
                    OnPropertyChanged(nameof(IsMarketDataLoginVisible));
                }
            }
        }

        public string MarketDataServerAddress
        {
            get => _marketDataServerAddress;
            set => SetProperty(ref _marketDataServerAddress, value);
        }

        public string MarketDataUsername
        {
            get => _marketDataUsername;
            set => SetProperty(ref _marketDataUsername, value);
        }

        public string MarketDataPassword
        {
            get => _marketDataPassword;
            set => SetProperty(ref _marketDataPassword, value);
        }

        public string MarketDataApiKey
        {
            get => _marketDataApiKey;
            set => SetProperty(ref _marketDataApiKey, value);
        }

        public bool IsLogging
        {
            get => _isLogging;
            set => SetProperty(ref _isLogging, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool RememberCredentials
        {
            get => _rememberCredentials;
            set => SetProperty(ref _rememberCredentials, value);
        }

        // 界面显示控制属性
        public bool IsMarketDataConfigVisible => SelectedMode == LoginMode.Separated;
        public bool IsApiKeyVisible => SelectedMarketDataSource == "myquant" || SelectedMarketDataSource == "ricequant";
        public bool IsMarketDataLoginVisible => SelectedMarketDataSource != "sina" && SelectedMarketDataSource != "eastmoney";

        public string ModeDescription => SelectedMode switch
        {
            LoginMode.BrokerDirect => "使用券商账户进行交易，同时从券商获取行情数据",
            LoginMode.Separated => "使用券商账户进行交易，从第三方平台获取行情数据",
            _ => ""
        };

        public ObservableCollection<LoginModeInfo> AvailableLoginModes { get; }
            = new ObservableCollection<LoginModeInfo>();

        public ObservableCollection<BrokerInfo> AvailableBrokers { get; }
            = new ObservableCollection<BrokerInfo>();

        public ObservableCollection<MarketDataSourceInfo> AvailableMarketDataSources { get; }
            = new ObservableCollection<MarketDataSourceInfo>();

        public bool CanLogin => !string.IsNullOrEmpty(SelectedBrokerType) &&
                                !string.IsNullOrEmpty(BrokerServerAddress) &&
                                !string.IsNullOrEmpty(BrokerUsername) &&
                                !string.IsNullOrEmpty(BrokerPassword) &&
                                (SelectedMode == LoginMode.BrokerDirect ||
                                 (!string.IsNullOrEmpty(SelectedMarketDataSource) && IsMarketDataConfigValid())) &&
                                !IsLogging;

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand TestBrokerConnectionCommand { get; }
        public ICommand TestMarketDataConnectionCommand { get; }

        public event Action<IBrokerService, IMarketDataService> LoginSuccessful;
        public event Action LoginCancelled;

        public LoginViewModel(BrokerServiceFactory brokerServiceFactory, MarketDataServiceFactory marketDataServiceFactory)
        {
            _brokerServiceFactory = brokerServiceFactory;
            _marketDataServiceFactory = marketDataServiceFactory;

            // 初始化集合
            InitializeLoginModes();
            InitializeAvailableBrokers();
            InitializeAvailableMarketDataSources();

            // 初始化命令
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, ()=> CanLogin);
            CancelCommand = new RelayCommand(()=> LoginCancelled?.Invoke());
            TestBrokerConnectionCommand = new AsyncRelayCommand(ExecuteTestBrokerConnectionAsync);
            TestMarketDataConnectionCommand = new AsyncRelayCommand(ExecuteTestMarketDataConnectionAsync);

            // 加载保存的设置
            LoadSavedSettings();
        }

        private void InitializeLoginModes()
        {
            AvailableLoginModes.Add(new LoginModeInfo
            {
                Mode = LoginMode.BrokerDirect,
                Name = "券商直连",
                Description = "交易和行情都通过券商接口"
            });

            AvailableLoginModes.Add(new LoginModeInfo
            {
                Mode = LoginMode.Separated,
                Name = "分离模式",
                Description = "券商交易 + 第三方行情"
            });

            // 默认选择券商直连
            SelectedMode = LoginMode.BrokerDirect;
        }

        private void InitializeAvailableBrokers()
        {
            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "simulated",
                Name = "模拟券商",
                Description = "用于测试的模拟券商",
                DefaultServerAddress = "localhost:8888",
                RequiresRealCredentials = false,
                SupportsMarketData = true
            });

            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "ctp",
                Name = "CTP期货",
                Description = "上期技术CTP期货交易接口",
                DefaultServerAddress = "tcp://180.168.146.187:10100",
                RequiresRealCredentials = true,
                SupportsMarketData = true
            });

            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "xtp",
                Name = "XTP股票",
                Description = "中泰证券XTP股票交易接口",
                DefaultServerAddress = "tcp://120.27.164.138:6001",
                RequiresRealCredentials = true,
                SupportsMarketData = true
            });

            // 默认选择模拟券商
            SelectedBrokerType = "simulated";
        }

        private void InitializeAvailableMarketDataSources()
        {
            AvailableMarketDataSources.Add(new MarketDataSourceInfo
            {
                Type = "simulated",
                Name = "模拟券商",
                Description = "用于测试的模拟券商",
                DefaultServerAddress = "localhost:8888",
                RequiresAuth = false,
                SupportsApiKey = false
            }); 
            
            AvailableMarketDataSources.Add(new MarketDataSourceInfo
            {
                Type = "sina",
                Name = "新浪财经",
                Description = "免费实时股票数据",
                RequiresAuth = false,
                DefaultServerAddress = "http://hq.sinajs.cn",
                SupportsApiKey = false
            });

            AvailableMarketDataSources.Add(new MarketDataSourceInfo
            {
                Type = "jukuan",
                Name = "掘金量化",
                Description = "专业量化数据平台",
                RequiresAuth = true,
                DefaultServerAddress = "https://www.myquant.cn",
                SupportsApiKey = true
            });

            AvailableMarketDataSources.Add(new MarketDataSourceInfo
            {
                Type = "xtp",
                Name = "XTP股票",
                Description = "中泰证券XTP股票交易接口",
                DefaultServerAddress = "tcp://120.27.164.138:6001",
                RequiresAuth = true,
                SupportsApiKey = true
            });

            // 默认选择新浪财经
            SelectedMarketDataSource = "simulated";
        }

        private void OnModeChanged()
        {
            if (SelectedMode == LoginMode.BrokerDirect)
            {
                // 券商直连模式，行情数据源与券商保持一致
                SelectedMarketDataSource = "broker";
            }
            else
            {
                // 分离模式，默认选择新浪财经
                if (SelectedMarketDataSource == "broker")
                {
                    SelectedMarketDataSource = "sina";
                }
            }
        }

        private void LoadDefaultBrokerSettings()
        {
            var broker = AvailableBrokers.FirstOrDefault(b => b.Type == SelectedBrokerType);
            if (broker != null)
            {
                BrokerServerAddress = broker.DefaultServerAddress;
            }
        }

        private void LoadDefaultMarketDataSettings()
        {
            var dataSource = AvailableMarketDataSources.FirstOrDefault(ds => ds.Type == SelectedMarketDataSource);
            if (dataSource != null)
            {
                MarketDataServerAddress = dataSource.DefaultServerAddress;
            }
        }

        private bool IsMarketDataConfigValid()
        {
            if (SelectedMode == LoginMode.BrokerDirect)
                return true;

            if (string.IsNullOrEmpty(SelectedMarketDataSource))
                return false;

            var dataSource = AvailableMarketDataSources.FirstOrDefault(ds => ds.Type == SelectedMarketDataSource);
            if (dataSource == null)
                return false;

            // 免费数据源不需要认证
            if (!dataSource.RequiresAuth)
                return true;

            // 需要API Key的数据源
            if (dataSource.SupportsApiKey)
                return !string.IsNullOrEmpty(MarketDataApiKey);

            // 需要用户名密码的数据源
            return !string.IsNullOrEmpty(MarketDataUsername) && !string.IsNullOrEmpty(MarketDataPassword);
        }

        private void LoadSavedSettings()
        {
            try
            {
                var settings = Properties.Settings.Default;

                // 加载登录模式
                if (Enum.TryParse<LoginMode>(settings.LoginMode, out var mode))
                {
                    SelectedMode = mode;
                }

                // 加载券商设置
                if (!string.IsNullOrEmpty(settings.BrokerType))
                {
                    SelectedBrokerType = settings.BrokerType;
                }

                if (!string.IsNullOrEmpty(settings.Username))
                {
                    BrokerUsername = settings.Username;
                    RememberCredentials = true;
                }

                if (!string.IsNullOrEmpty(settings.ServerAddress))
                {
                    BrokerServerAddress = settings.ServerAddress;
                }

                // 加载行情数据源设置
                if (!string.IsNullOrEmpty(settings.MarketDataSource))
                {
                    SelectedMarketDataSource = settings.MarketDataSource;
                }

                if (!string.IsNullOrEmpty(settings.MarketDataServerAddress))
                {
                    MarketDataServerAddress = settings.MarketDataServerAddress;
                }

                if (!string.IsNullOrEmpty(settings.MarketDataUsername))
                {
                    MarketDataUsername = settings.MarketDataUsername;
                }

                if (!string.IsNullOrEmpty(settings.MarketDataApiKey))
                {
                    MarketDataApiKey = settings.MarketDataApiKey;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载保存的设置时出错: {ex.Message}";
            }
        }

        private void SaveSettings()
        {
            if (!RememberCredentials)
                return;

            try
            {
                var settings = Properties.Settings.Default;
                settings.LoginMode = SelectedMode.ToString();
                settings.BrokerType = SelectedBrokerType;
                settings.Username = BrokerUsername;
                settings.ServerAddress = BrokerServerAddress;
                settings.MarketDataSource = SelectedMarketDataSource;
                settings.MarketDataServerAddress = MarketDataServerAddress;
                settings.MarketDataUsername = MarketDataUsername;
                settings.MarketDataApiKey = MarketDataApiKey;
                // 注意：不保存密码和敏感信息
                settings.Save();
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存设置时出错: {ex.Message}";
            }
        }

        private async Task ExecuteLoginAsync()
        {
            if (IsLogging)
                return;

            try
            {
                IsLogging = true;
                StatusMessage = "正在连接...";

                // 验证输入
                if (!ValidateInput())
                    return;

                // 创建券商服务
                StatusMessage = "正在连接券商...";
                var brokerService = _brokerServiceFactory.CreateBrokerService(SelectedBrokerType);
                bool brokerConnected = await brokerService.ConnectAsync(BrokerUsername, BrokerPassword, BrokerServerAddress);

                if (!brokerConnected)
                {
                    StatusMessage = "券商连接失败，请检查账户信息";
                    return;
                }

                // 创建行情数据服务
                IMarketDataService marketDataService;

                if (SelectedMode == LoginMode.BrokerDirect)
                {
                    // 券商直连模式，行情数据来自券商
                    StatusMessage = "正在获取券商行情权限...";
                    marketDataService = brokerService.MarketDataService;
                    if (brokerService.MarketDataService is null)
                    {
                        // 如果券商不支持行情数据，回退到免费数据源
                        marketDataService = _marketDataServiceFactory.CreateMarketDataService("sina");
                    }
                }
                else
                {
                    // 分离模式，使用第三方行情数据
                    StatusMessage = "正在连接行情数据源...";
                    marketDataService = await CreateMarketDataServiceAsync();

                    if (marketDataService == null)
                    {
                        StatusMessage = "行情数据源连接失败";
                        await brokerService.DisconnectAsync();
                        return;
                    }
                    brokerService.SetMarketDataService(marketDataService);
                }

                StatusMessage = "登录成功！";

                // 保存设置
                SaveSettings();

                // 延迟一下让用户看到成功消息
                await Task.Delay(500);

                // 触发登录成功事件
                LoginSuccessful?.Invoke(brokerService, marketDataService);
            }
            catch (Exception ex)
            {
                StatusMessage = $"登录失败: {ex.Message}";
            }
            finally
            {
                IsLogging = false;
            }
        }

        private async Task<IMarketDataService> CreateMarketDataServiceAsync()
        {
            var dataSource = AvailableMarketDataSources.FirstOrDefault(ds => ds.Type == SelectedMarketDataSource);
            if (dataSource == null)
                return null;

            try
            {
                if (!dataSource.RequiresAuth)
                {
                    // 免费数据源，直接创建
                    return _marketDataServiceFactory.CreateMarketDataService(SelectedMarketDataSource);
                }
                else
                {
                    // 需要认证的数据源
                    var marketDataService = _marketDataServiceFactory.CreateMarketDataService(SelectedMarketDataSource);

                    if (marketDataService is IAuthenticatableMarketDataService authService)
                    {
                        bool authResult;

                        if (dataSource.SupportsApiKey)
                        {
                            authResult = await authService.AuthenticateAsync(MarketDataApiKey);
                        }
                        else
                        {
                            authResult = await authService.AuthenticateAsync(MarketDataUsername, MarketDataPassword, MarketDataServerAddress);
                        }

                        if (!authResult)
                        {
                            return null;
                        }
                    }

                    return marketDataService;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建行情数据服务失败: {ex.Message}");
                return null;
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrEmpty(SelectedBrokerType))
            {
                StatusMessage = "请选择券商类型";
                return false;
            }

            if (string.IsNullOrEmpty(BrokerUsername) || string.IsNullOrEmpty(BrokerPassword))
            {
                StatusMessage = "请输入券商用户名和密码";
                return false;
            }

            if (SelectedMode == LoginMode.Separated && !IsMarketDataConfigValid())
            {
                StatusMessage = "请正确配置行情数据源";
                return false;
            }

            return true;
        }

        private async Task ExecuteTestBrokerConnectionAsync()
        {
            try
            {
                StatusMessage = "正在测试券商连接...";

                var brokerService = _brokerServiceFactory.CreateBrokerService(SelectedBrokerType);
                bool connected = await brokerService.ConnectAsync(BrokerUsername, BrokerPassword, BrokerServerAddress);

                if (connected)
                {
                    StatusMessage = "券商连接测试成功";
                    await brokerService.DisconnectAsync();
                }
                else
                {
                    StatusMessage = "券商连接测试失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"券商连接测试失败: {ex.Message}";
            }
        }

        private async Task ExecuteTestMarketDataConnectionAsync()
        {
            try
            {
                StatusMessage = "正在测试行情数据连接...";

                var marketDataService = await CreateMarketDataServiceAsync();

                if (marketDataService != null)
                {
                    // 尝试获取一个测试数据
                    var testData = await marketDataService.GetLevel1DataAsync("000001");

                    if (testData != null)
                    {
                        StatusMessage = "行情数据连接测试成功";
                    }
                    else
                    {
                        StatusMessage = "行情数据连接测试失败：无法获取数据";
                    }
                }
                else
                {
                    StatusMessage = "行情数据连接测试失败";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"行情数据连接测试失败: {ex.Message}";
            }
        }
    }
}
