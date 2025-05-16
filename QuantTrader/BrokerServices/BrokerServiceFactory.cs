using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
                "simulated" => _serviceProvider.GetRequiredService<SimulatedBrokerService>(),
                // 可以在这里添加其他券商的支持
                // "ctp" => _serviceProvider.GetRequiredService<CtpBrokerService>(),
                // "xtp" => _serviceProvider.GetRequiredService<XtpBrokerService>(),
                _ => throw new ArgumentException($"Unsupported broker type: {brokerType}")
            };
        }
    }
}
