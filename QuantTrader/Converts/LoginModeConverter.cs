using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Models;
using System.Windows.Data;

namespace QuantTrader.Converts
{
    public class LoginModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LoginMode mode && parameter is System.Collections.ObjectModel.ObservableCollection<LoginModeInfo> modes)
            {
                return modes.FirstOrDefault(m => m.Mode == mode);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LoginModeInfo modeInfo)
            {
                return modeInfo.Mode;
            }
            return LoginMode.BrokerDirect;
        }
    }
}
