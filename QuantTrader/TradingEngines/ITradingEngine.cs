using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Models;
using QuantTrader.Strategies;

namespace QuantTrader.TradingEngines
{
    public interface ITradingEngine
    {
        /// <summary>
        /// 当前策略列表
        /// </summary>
        IReadOnlyList<StrategyBase> Strategies { get; }

        /// <summary>
        /// 当前账户信息
        /// </summary>
        Account Account { get; }

        /// <summary>
        /// 启动交易引擎
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止交易引擎
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 获取策略
        /// </summary>
        StrategyBase GetStrategy(string strategyId);

        /// <summary>
        /// 启动策略
        /// </summary>
        Task StartStrategyAsync(string strategyId);

        /// <summary>
        /// 停止策略
        /// </summary>
        Task StopStrategyAsync(string strategyId);

        /// <summary>
        /// 添加策略
        /// </summary>
        Task<IStrategy> AddStrategyAsync(string strategyType, Dictionary<string, object> parameters = null);

        /// <summary>
        /// 更新策略参数
        /// </summary>
        //Task UpdateStrategyParametersAsync(string strategyId, Dictionary<string, object> parameters);

        /// <summary>
        /// 信号生成事件
        /// </summary>
        event Action<string, Signal> SignalGenerated;

        /// <summary>
        /// 订单执行事件
        /// </summary>
        event Action<string, Order> OrderExecuted;

        /// <summary>
        /// 策略日志事件
        /// </summary>
        event Action<string, string> StrategyLogGenerated;

        /// <summary>
        /// 账户更新事件
        /// </summary>
        event Action<Account> AccountUpdated;
    }
}
