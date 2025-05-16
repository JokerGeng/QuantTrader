using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.ViewModels
{
    /// <summary>
    /// 策略参数视图模型
    /// </summary>
    public class StrategyParameterViewModel : ViewModelBase
    {
        private string _name;
        private object _value;
        private string _description;
        private string _type;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public object Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public StrategyParameterViewModel(string name, object value, string description, string type)
        {
            Name = name;
            Value = value;
            Description = description;
            Type = type;
        }
    }
}
