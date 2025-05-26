using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.ViewModels;

namespace QuantTrader
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public IBrokerService BrokerService { get; private set; }
        public IMarketDataService MarketDataService { get; private set; }

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            // 订阅事件
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.LoginCancelled += OnLoginCancelled;

            // 设置密码框初始值
            if (!string.IsNullOrEmpty(_viewModel.BrokerPassword))
            {
                BrokerPasswordBox.Password = _viewModel.BrokerPassword;
            }

            if (!string.IsNullOrEmpty(_viewModel.MarketDataPassword))
            {
                MarketDataPasswordBox.Password = _viewModel.MarketDataPassword;
            }
        }

        private void OnLoginSuccessful(IBrokerService brokerService, IMarketDataService marketDataService)
        {
            BrokerService = brokerService;
            MarketDataService = marketDataService;
            DialogResult = true;
            Close();
        }

        private void OnLoginCancelled()
        {
            DialogResult = false;
            Close();
        }

        private void BrokerPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.BrokerPassword = BrokerPasswordBox.Password;
        }

        private void MarketDataPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.MarketDataPassword = MarketDataPasswordBox.Password;
        }
    }
}
