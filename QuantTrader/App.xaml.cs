using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.TradingEngine;
using QuantTrader.ViewModels;

namespace QuantTrader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        // 公开ServiceProvider供视图使用
        public IServiceProvider ServiceProvider => _serviceProvider;

        public App()
        {
            // 配置依赖注入
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 配置数据路径
            var dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "QuantTrader",
                "Data");

            // 注册服务
            services.AddSingleton<IMarketDataService, SimulatedMarketDataService>();
            services.AddSingleton<IDataRepository>(provider => new CsvDataRepository(dataPath));
            services.AddSingleton<IBrokerService>(provider =>
                new SimulatedBrokerService(provider.GetRequiredService<IMarketDataService>()));
            services.AddSingleton<BrokerServiceFactory>();
            services.AddSingleton<ITradingEngine, TradingEngine.TradingEngine>();

            // 注册视图模型
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ChartViewModel>();

            // 注册视图
            services.AddScoped<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 创建并显示主窗口
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
