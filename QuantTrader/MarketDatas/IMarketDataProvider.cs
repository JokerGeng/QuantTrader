using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 行情数据提供者接口
    /// </summary>
    public interface IMarketDataProvider
    {
        /// <summary>
        /// 获取行情数据服务
        /// </summary>
        IMarketDataService GetMarketDataService();
    }
}
