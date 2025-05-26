using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    // 辅助类
    public class LoginModeInfo
    {
        public LoginMode Mode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public override string ToString() => Name;
    }
}
