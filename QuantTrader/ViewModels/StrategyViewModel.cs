using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantTrader.Strategies;

namespace QuantTrader.ViewModels
{
    public class StrategyViewModel : ViewModelBase
    {
        private string _id;
        private string _name;
        private string _description;
        private StrategyStatus _status;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public StrategyStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        public StrategyViewModel()
        {
        }

        public StrategyViewModel(IStrategy strategy)
        {
            Id = strategy.Id;
            Name = strategy.Name;
            Description = strategy.Description;
            Status = strategy.Status;

            foreach (var param in strategy.Parameters)
            {
                Parameters[param.Key] = param.Value;
            }
        }
    }
}
