using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OxyPlot.Series;
using QuantTrader.BrokerServices;
using QuantTrader.MarketDatas;
using QuantTrader.TradingEngines;
using QuantTrader.ViewModels;

namespace QuantTrader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private IBrokerService _brokerService;
        private IMarketDataService _marketDataService;

        // 公开ServiceProvider供视图使用
        public IServiceProvider ServiceProvider => _serviceProvider;
        public App()
        {
        }

        private bool Login()
        {
            bool flag = false;
            try
            {
                var services = new ServiceCollection();
                //注册服务

                // 配置数据路径
                var dataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "QuantTrader",
                    "Data");
                services.AddSingleton<BrokerServiceFactory>();
                services.AddSingleton<MarketDataServiceFactory>();
                services.AddSingleton<IDataRepository>(provider => new CsvDataRepository(dataPath));
                //注册模型模型
                services.AddSingleton<LoginViewModel>();
                //注册登录视图
                services.AddScoped<LoginWindow>();


                var loginWindow = services.BuildServiceProvider().GetRequiredService<LoginWindow>();
                var result = loginWindow.ShowDialog();

                if (result == true && loginWindow.BrokerService != null)
                {
                    // 登录成功，注册券商服务实例
                    _brokerService = loginWindow.BrokerService;
                    _marketDataService = loginWindow.MarketDataService;
                    //配置服务，包含券商服务
                    ConfigureServicesWithBroker(services, _brokerService, _marketDataService);
                    flag = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return flag;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (Login())
            {
                // 显示主窗口
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }

        private void ConfigureServicesWithBroker(IServiceCollection services, IBrokerService brokerService,IMarketDataService marketDataService)
        {
            // 注册已登录的券商服务实例
            services.AddSingleton<IBrokerService>(brokerService);
            services.AddSingleton<IMarketDataService>(marketDataService);

            // 注册交易引擎
            services.AddSingleton<ITradingEngine, TradingEngine>();

            // 注册视图模型
            services.AddSingleton<MainViewModel>();
            services.AddTransient<ChartViewModel>();

            // 注册视图
            services.AddScoped<MainWindow>();
            this._serviceProvider = services.BuildServiceProvider();
        }
    }
}
