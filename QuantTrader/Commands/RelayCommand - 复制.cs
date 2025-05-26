using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace QuantTrader.Commands
{
    public class RelayCommand_bak : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand_bak(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public partial class RelayCommand_bak<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand_bak(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is null && !typeof(T).IsValueType)
            {
                return _canExecute?.Invoke(default(T)) ?? true;
            }

            // Type safety check
            if (parameter is not null && parameter is not T)
            {
                return false;
            }

            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                // Handle null parameter gracefully
                if (parameter is null && !typeof(T).IsValueType)
                {
                    _execute(default(T));
                    return;
                }

                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
            }

        }
    }
}
