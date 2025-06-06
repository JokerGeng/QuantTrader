﻿using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    /// <summary>
    /// 作为模板
    /// </summary>
    public interface IStrategyInfo
    {

        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 策略描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 策略参数列表
        /// </summary>
        List<StrategyParameter> Parameters { get; }
    }
}
