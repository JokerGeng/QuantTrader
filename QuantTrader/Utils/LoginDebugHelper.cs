using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.ViewModels;

namespace QuantTrader.Utils
{
    public class LoginDebugHelper
    {
        public static void LogLoginState(LoginViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine($"=== 登录状态调试 ===");
            System.Diagnostics.Debug.WriteLine($"SelectedMode: {viewModel.SelectedMode}");
            System.Diagnostics.Debug.WriteLine($"SelectedBrokerType: {viewModel.SelectedBrokerInfo.Type}");
            System.Diagnostics.Debug.WriteLine($"BrokerServerAddress: {viewModel.SelectedBrokerInfo.DefaultServerAddress}");
            System.Diagnostics.Debug.WriteLine($"BrokerUsername: {viewModel.BrokerUsername}");
            System.Diagnostics.Debug.WriteLine($"BrokerPassword length: {viewModel.BrokerPassword?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"SelectedMarketDataSource: {viewModel.SelectedMarketDataSource}");
            System.Diagnostics.Debug.WriteLine($"IsMarketDataConfigVisible: {viewModel.SelectedMode.ToString()}");
            System.Diagnostics.Debug.WriteLine($"IsLogging: {viewModel.IsLogging}");
            System.Diagnostics.Debug.WriteLine($"CanLogin: {viewModel.CanLogin}");
            System.Diagnostics.Debug.WriteLine($"===================");
        }
    }
}
