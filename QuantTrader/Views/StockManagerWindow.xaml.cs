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
using QuantTrader.ViewModels;

namespace QuantTrader.Views
{
    /// <summary>
    /// StockManagerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StockManagerWindow : Window
    {
        private readonly StockManagerViewModel _viewModel;

        public StockManagerWindow(StockManagerViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void QuickAdd_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string symbol)
            {
                // 从股票数据库中查找对应的股票信息
                var stockInfo = _viewModel.SearchResults.FirstOrDefault(s => s.Symbol == symbol);
                if (stockInfo == null)
                {
                    // 如果搜索结果中没有，从数据库中查找
                    _viewModel.SearchText = symbol;
                    // 等待搜索完成后再添加
                    System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            var foundStock = _viewModel.SearchResults.FirstOrDefault(s => s.Symbol == symbol);
                            if (foundStock != null)
                            {
                                _viewModel.AddStockCommand.Execute(foundStock);
                            }
                        });
                    });
                }
                else
                {
                    _viewModel.AddStockCommand.Execute(stockInfo);
                }
            }
        }
    }
}
