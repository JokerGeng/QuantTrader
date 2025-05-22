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
                "eastmoney" => new EastmoneyMarketDataService(),
                _ => throw new ArgumentException($"Unsupported data source: {dataSource}")
            };
        }

        /// <summary>
        /// 获取支持的数据源列表
        /// </summary>
        public static string[] GetSupportedDataSources()
        {
            return new[] { "simulated", "sina", "eastmoney" };
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
