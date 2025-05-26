using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.MarketDatas
{
    /// <summary>
    /// 可认证的行情数据服务接口
    /// </summary>
    public interface IAuthenticatableMarketDataService : IMarketDataService
    {
        /// <summary>
        /// 使用API Key认证
        /// </summary>
        Task<bool> AuthenticateAsync(string apiKey);

        /// <summary>
        /// 使用用户名密码认证
        /// </summary>
        Task<bool> AuthenticateAsync(string username, string password, string serverAddress);

        /// <summary>
        /// 是否已认证
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
