using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public class BrokerConnectionInfo
    {
        public string BrokerType { get; set; }
        public string BrokerName { get; set; }
        public string Username { get; set; }
        public string ServerAddress { get; set; }
        public DateTime ConnectedTime { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            return $"{BrokerName} - {Username}@{ServerAddress}";
        }
    }
}
