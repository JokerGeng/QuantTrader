using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public interface IStrategyRuntime
    {
        IStrategyInfo StrategyInfo { get; }

        /// <summary>
        /// 运行策略的股票
        /// </summary>
        string Symbol { get; }

        /// <summary>
        /// 策略状态
        /// </summary>
        StrategyStatus Status { get; }

        /// <summary>
        /// 初始化策略
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 启动策略
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// 停止策略
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// 订单执行事件
        /// </summary>
        event Action<string, Order> OrderExecuted;

        /// <summary>
        /// 信号生成事件
        /// </summary>
        event Action<string, Signal> SignalGenerated;

        /// <summary>
        /// 策略日志事件
        /// </summary>
        event Action<string, string> LogGenerated;
    }
}
