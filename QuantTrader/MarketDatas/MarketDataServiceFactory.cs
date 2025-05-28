using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.BrokerServices;

namespace QuantTrader.MarketDatas
{
    public class MarketDataServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public MarketDataServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 创建市场数据服务
        /// </summary>
        public IMarketDataService CreateMarketDataService(string dataSource)
        {
            return dataSource.ToLower() switch
            {
                "simulated" => new SimulatedMarketDataService(),
                "sina" => new SinaMarketDataService(),
                "jukuan" => new JukuanMarketDataService(),
                "xtp" => new XtpMarketDataService(),
                "broker" => CreateBrokerMarketDataService(), // 券商行情数据
                _ => throw new ArgumentException($"Unsupported data source: {dataSource}")
            };
        }

        /// <summary>
        /// 创建券商行情数据服务（当选择券商直连模式时）
        /// </summary>
        private IMarketDataService CreateBrokerMarketDataService()
        {
            // 这里返回一个占位符服务，实际使用时会被券商服务的行情数据替换
            return new SimulatedMarketDataService();
        }

        /// <summary>
        /// 获取支持的数据源列表
        /// </summary>
        public static string[] GetSupportedDataSources()
        {
            return new[] { "simulated", "broker", "jukuan"};
        }

        /// <summary>
        /// 检查数据源是否支持
        /// </summary>
        public static bool IsDataSourceSupported(string dataSource)
        {
            return Array.Exists(GetSupportedDataSources(),
                source => source.Equals(dataSource, StringComparison.OrdinalIgnoreCase));
        }
    }
}
