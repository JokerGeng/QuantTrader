using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QuantTrader.Converts
{
    public class BrokerDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string brokerType)
            {
                return brokerType switch
                {
                    "simulated" => "用于测试的模拟券商，无需真实账户",
                    "ctp" => "上期技术CTP期货交易接口，支持期货交易",
                    "xtp" => "中泰证券XTP股票交易接口，支持股票交易",
                    _ => "Unknown broker type"
                };
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
