﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.BrokerServices
{
    public class BrokerInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string DefaultServerAddress { get; set; }
        public bool RequiresRealCredentials { get; set; }
        public bool RequiresAuth { get; set; }
        public bool SupportsApiKey { get; set; }
        public bool SupportsMarketData { get; internal set; }

        public override string ToString() => Name;
    }
}
