using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public enum LoginMode
    {
        /// <summary>
        /// 券商直连模式 - 交易和行情都来自券商
        /// </summary>
        BrokerDirect,

        /// <summary>
        /// 分离模式 - 券商交易 + 第三方行情
        /// </summary>
        Separated

    }
}
