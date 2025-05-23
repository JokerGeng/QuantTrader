using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    public class StrategyTemplateViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> DefaultParameters { get; set; } = new Dictionary<string, object>();
    }
}
