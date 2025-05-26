using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public class LoginConfiguration
    {
        public LoginMode Mode { get; set; }

        // 券商配置
        public string BrokerType { get; set; }
        public string BrokerUsername { get; set; }
        public string BrokerPassword { get; set; }
        public string BrokerServerAddress { get; set; }

        // 行情数据源配置
        public string MarketDataSource { get; set; }
        public string MarketDataUsername { get; set; }
        public string MarketDataPassword { get; set; }
        public string MarketDataServerAddress { get; set; }
        public string MarketDataApiKey { get; set; }
    }
}
