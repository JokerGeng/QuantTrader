using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;
using QuantTrader.Commands;
using System.Windows.Input;

namespace QuantTrader.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly BrokerServiceFactory _brokerServiceFactory;

        private string _selectedBrokerType;
        private string _serverAddress;
        private string _username;
        private string _password;
        private bool _isLogging;
        private string _statusMessage;
        private bool _rememberCredentials;

        public string SelectedBrokerType
        {
            get => _selectedBrokerType;
            set
            {
                if (SetProperty(ref _selectedBrokerType, value))
                {
                    // 根据券商类型加载默认服务器地址
                    LoadDefaultServerAddress();
                    OnPropertyChanged(nameof(CanLogin));
                }
            }
        }

        public string ServerAddress
        {
            get => _serverAddress;
            set
            {
                SetProperty(ref _serverAddress, value);
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        [Required(ErrorMessage = "Username is required")]
        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                OnPropertyChanged(nameof(CanLogin));
            }
        }

        [Required(ErrorMessage = "Password is required")]
        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                OnPropertyChanged(nameof(CanLogin));
            }
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

        public ObservableCollection<BrokerInfo> AvailableBrokers { get; }
            = new ObservableCollection<BrokerInfo>();

        public bool CanLogin => !string.IsNullOrEmpty(SelectedBrokerType) &&
                                !string.IsNullOrEmpty(ServerAddress) &&
                                !string.IsNullOrEmpty(Username) &&
                                !string.IsNullOrEmpty(Password) &&
                                !IsLogging;

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public event Action<IBrokerService> LoginSuccessful;
        public event Action LoginCancelled;

        public LoginViewModel(BrokerServiceFactory brokerServiceFactory)
        {
            _brokerServiceFactory = brokerServiceFactory;

            // 初始化可用券商列表
            InitializeAvailableBrokers();

            // 初始化命令
            LoginCommand = new AsyncRelayCommand(_ => ExecuteLoginAsync(), _ => CanLogin);
            CancelCommand = new RelayCommand(_ => LoginCancelled?.Invoke());

            // 加载保存的凭据
            LoadSavedCredentials();
        }

        private void InitializeAvailableBrokers()
        {
            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "simulated",
                Name = "模拟券商",
                Description = "用于测试的模拟券商",
                DefaultServerAddress = "localhost:8888",
                RequiresRealCredentials = false
            });

            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "ctp",
                Name = "CTP",
                Description = "上期技术CTP期货交易接口",
                DefaultServerAddress = "tcp://180.168.146.187:10100",
                RequiresRealCredentials = true
            });

            AvailableBrokers.Add(new BrokerInfo
            {
                Type = "xtp",
                Name = "XTP",
                Description = "中泰证券XTP股票交易接口",
                DefaultServerAddress = "tcp://120.27.164.138:6001",
                RequiresRealCredentials = true
            });

            // 默认选择模拟券商
            if (AvailableBrokers.Count > 0)
            {
                var savedBrokerType = Properties.Settings.Default.BrokerType;
                var savedBroker = AvailableBrokers.FirstOrDefault(b => b.Type == savedBrokerType);
                SelectedBrokerType = savedBroker?.Type ?? AvailableBrokers[0].Type;
            }
        }

        private void LoadDefaultServerAddress()
        {
            var broker = AvailableBrokers.FirstOrDefault(b => b.Type == SelectedBrokerType);
            if (broker != null)
            {
                ServerAddress = broker.DefaultServerAddress;
            }
        }

        private void LoadSavedCredentials()
        {
            try
            {
                var settings = Properties.Settings.Default;

                if (!string.IsNullOrEmpty(settings.Username))
                {
                    Username = settings.Username;
                    RememberCredentials = true;
                }

                if (!string.IsNullOrEmpty(settings.ServerAddress))
                {
                    ServerAddress = settings.ServerAddress;
                }

                // 注意：为了安全，不保存密码
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading saved credentials: {ex.Message}";
            }
        }

        private void SaveCredentials()
        {
            if (!RememberCredentials)
                return;

            try
            {
                var settings = Properties.Settings.Default;
                settings.BrokerType = SelectedBrokerType;
                settings.Username = Username;
                settings.ServerAddress = ServerAddress;
                settings.Save();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving credentials: {ex.Message}";
            }
        }

        private async Task ExecuteLoginAsync()
        {
            if (IsLogging)
                return;

            try
            {
                IsLogging = true;
                StatusMessage = "Connecting to broker...";

                // 验证输入
                if (!ValidateInput())
                    return;

                // 创建券商服务
                var brokerService = _brokerServiceFactory.CreateBrokerService(SelectedBrokerType);

                // 连接到券商
                bool connected = await brokerService.ConnectAsync(Username, Password, ServerAddress);

                if (connected)
                {
                    StatusMessage = "Login successful!";

                    // 保存凭据
                    SaveCredentials();

                    // 触发登录成功事件
                    await Task.Delay(500); // 显示成功消息一会儿
                    LoginSuccessful?.Invoke(brokerService);
                }
                else
                {
                    StatusMessage = "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Login error: {ex.Message}";
            }
            finally
            {
                IsLogging = false;
            }
        }

        private bool ValidateInput()
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);

            bool isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

            if (!isValid)
            {
                StatusMessage = validationResults.First().ErrorMessage;
                return false;
            }

            // 额外验证
            if (string.IsNullOrEmpty(SelectedBrokerType))
            {
                StatusMessage = "Please select a broker type.";
                return false;
            }

            if (string.IsNullOrEmpty(ServerAddress))
            {
                StatusMessage = "Server address is required.";
                return false;
            }

            return true;
        }
    }
}
