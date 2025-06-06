﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Models;

namespace QuantTrader.Strategies
{
    public interface IStrategy
    {
        /// <summary>
        /// 策略ID
        /// </summary>
        string Id { get; }

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
        /// 更新策略参数
        /// </summary>
        Task UpdateParametersAsync(StrategyParameter parameter);

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
