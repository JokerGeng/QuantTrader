using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Utils
{
    /// <summary>
    /// 技术指标计算器
    /// </summary>
    public static class IndicatorCalculator
    {
        /// <summary>
        /// 计算简单移动平均线
        /// </summary>
        public static decimal[] SMA(decimal[] prices, int period)
        {
            if (prices == null || prices.Length == 0)
                return Array.Empty<decimal>();

            if (period <= 0)
                throw new ArgumentException("Period must be positive", nameof(period));

            var result = new decimal[prices.Length];

            for (int i = 0; i < prices.Length; i++)
            {
                if (i < period - 1)
                {
                    // 数据不足一个周期，返回价格本身
                    result[i] = prices[i];
                    continue;
                }

                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    sum += prices[i - j];
                }

                result[i] = sum / period;
            }

            return result;
        }

        /// <summary>
        /// 计算指数移动平均线
        /// </summary>
        public static decimal[] EMA(decimal[] prices, int period)
        {
            if (prices == null || prices.Length == 0)
                return Array.Empty<decimal>();

            if (period <= 0)
                throw new ArgumentException("Period must be positive", nameof(period));

            var result = new decimal[prices.Length];
            var k = 2m / (period + 1); // 平滑系数

            // 第一个值使用简单平均
            decimal sum = 0;
            int count = Math.Min(period, prices.Length);
            for (int i = 0; i < count; i++)
            {
                sum += prices[i];
            }
            result[0] = sum / count;

            // 计算其余值
            for (int i = 1; i < prices.Length; i++)
            {
                result[i] = prices[i] * k + result[i - 1] * (1 - k);
            }

            return result;
        }

        /// <summary>
        /// 计算布林带
        /// </summary>
        public static (decimal[] Middle, decimal[] Upper, decimal[] Lower) BollingerBands(decimal[] prices, int period, decimal multiplier)
        {
            if (prices == null || prices.Length == 0)
                return (Array.Empty<decimal>(), Array.Empty<decimal>(), Array.Empty<decimal>());

            if (period <= 0)
                throw new ArgumentException("Period must be positive", nameof(period));

            var middle = SMA(prices, period);
            var upper = new decimal[prices.Length];
            var lower = new decimal[prices.Length];

            for (int i = period - 1; i < prices.Length; i++)
            {
                // 计算标准差
                decimal sum = 0;
                for (int j = 0; j < period; j++)
                {
                    decimal deviation = prices[i - j] - middle[i];
                    sum += deviation * deviation;
                }
                decimal stdDev = (decimal)Math.Sqrt((double)(sum / period));

                upper[i] = middle[i] + multiplier * stdDev;
                lower[i] = middle[i] - multiplier * stdDev;
            }

            return (middle, upper, lower);
        }

        /// <summary>
        /// 计算MACD
        /// </summary>
        public static (decimal[] MACD, decimal[] Signal, decimal[] Histogram) MACD(decimal[] prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            if (prices == null || prices.Length == 0)
                return (Array.Empty<decimal>(), Array.Empty<decimal>(), Array.Empty<decimal>());

            if (fastPeriod <= 0 || slowPeriod <= 0 || signalPeriod <= 0)
                throw new ArgumentException("Periods must be positive");

            if (fastPeriod >= slowPeriod)
                throw new ArgumentException("Fast period must be less than slow period");

            var fastEMA = EMA(prices, fastPeriod);
            var slowEMA = EMA(prices, slowPeriod);

            var macdLine = new decimal[prices.Length];
            for (int i = 0; i < prices.Length; i++)
            {
                macdLine[i] = fastEMA[i] - slowEMA[i];
            }

            var signalLine = EMA(macdLine, signalPeriod);

            var histogram = new decimal[prices.Length];
            for (int i = 0; i < prices.Length; i++)
            {
                histogram[i] = macdLine[i] - signalLine[i];
            }

            return (macdLine, signalLine, histogram);
        }

        /// <summary>
        /// 计算RSI
        /// </summary>
        public static decimal[] RSI(decimal[] prices, int period)
        {
            if (prices == null || prices.Length == 0)
                return Array.Empty<decimal>();

            if (period <= 0)
                throw new ArgumentException("Period must be positive", nameof(period));

            var result = new decimal[prices.Length];

            if (prices.Length <= period)
            {
                // 数据不足一个周期，返回50
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = 50;
                }
                return result;
            }

            // 计算第一个值的初始RS
            decimal sumGain = 0;
            decimal sumLoss = 0;

            for (int i = 1; i <= period; i++)
            {
                decimal change = prices[i] - prices[i - 1];
                if (change > 0)
                    sumGain += change;
                else
                    sumLoss -= change; // 转为正值
            }

            decimal avgGain = sumGain / period;
            decimal avgLoss = sumLoss / period;

            // 第一个RSI值
            decimal rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            result[period] = 100 - (100 / (1 + rs));

            // 计算其余RSI值
            for (int i = period + 1; i < prices.Length; i++)
            {
                decimal change = prices[i] - prices[i - 1];
                decimal gain = Math.Max(0, change);
                decimal loss = Math.Max(0, -change);

                // 使用Wilder平滑法
                avgGain = ((avgGain * (period - 1)) + gain) / period;
                avgLoss = ((avgLoss * (period - 1)) + loss) / period;

                rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
                result[i] = 100 - (100 / (1 + rs));
            }

            return result;
        }
    }
}
