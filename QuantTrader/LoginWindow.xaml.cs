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

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            // 订阅事件
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.LoginCancelled += OnLoginCancelled;

            // 设置初始焦点
            Loaded += (s, e) =>
            {
                if (string.IsNullOrEmpty(_viewModel.Username))
                {
                    // 如果用户名为空，焦点设置到用户名输入框
                    var usernameTextBox = FindName("UsernameTextBox") as System.Windows.Controls.TextBox;
                    usernameTextBox?.Focus();
                }
                else
                {
                    // 如果用户名已填，焦点设置到密码输入框
                    PasswordBox.Focus();
                }
            };
        }

        private void OnLoginSuccessful(IBrokerService brokerService)
        {
            BrokerService = brokerService;
            DialogResult = true;
            Close();
        }

        private void OnLoginCancelled()
        {
            DialogResult = false;
            Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // 同步密码到视图模型
            _viewModel.Password = PasswordBox.Password;
        }
    }
}
