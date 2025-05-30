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
using QuantTrader.Models;
using QuantTrader.ViewModels;

namespace QuantTrader.Views
{
    /// <summary>
    /// StrategyConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StrategyConfigWindow : Window
    {
        private readonly StrategyConfigViewModel _viewModel;

        public StrategyConfigViewModel ViewModel => _viewModel;
        public StrategyConfigWindow()
        {
            InitializeComponent();

            _viewModel = new StrategyConfigViewModel();
            DataContext = _viewModel;

            // 订阅事件
            _viewModel.SaveRequested += OnSaveRequested;
            _viewModel.CancelRequested += OnCancelRequested;
        }

        private void OnSaveRequested()
        {
            DialogResult = true;
            Close();
        }

        private void OnCancelRequested()
        {
            DialogResult = false;
            Close();
        }
    }
}
