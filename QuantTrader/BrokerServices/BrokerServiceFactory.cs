using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using QuantTrader.MarketDatas;

namespace QuantTrader.BrokerServices
{
    public class BrokerServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public BrokerServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 创建券商服务
        /// </summary>
        public IBrokerService CreateBrokerService(string brokerType)
        {
            return brokerType.ToLower() switch
            {
                "simulated" => new SimulatedBrokerService(_serviceProvider.GetRequiredService<IMarketDataService>()),
                "ctp" => new CtpBrokerService(),
                "xtp" => throw new NotImplementedException("XTP broker service not implemented yet"),
                _ => throw new ArgumentException($"Unsupported broker type: {brokerType}")
            };
        }

        /// <summary>
        /// 获取支持的券商类型列表
        /// </summary>
        public static string[] GetSupportedBrokerTypes()
        {
            return new[] { "simulated", "ctp", "xtp" };
        }

        /// <summary>
        /// 检查券商类型是否支持
        /// </summary>
        public static bool IsBrokerTypeSupported(string brokerType)
        {
            return Array.Exists(GetSupportedBrokerTypes(),
                type => type.Equals(brokerType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
