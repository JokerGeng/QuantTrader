using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuantTrader.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute)
        {
            ArgumentNullException.ThrowIfNull(execute); 
            this._execute = execute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            ArgumentNullException.ThrowIfNull(execute);
            ArgumentNullException.ThrowIfNull(canExecute);
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return this._canExecute?.Invoke() != false;
        }

        public void Execute(object? parameter)
        {
            this._execute();
        }
    }

}
