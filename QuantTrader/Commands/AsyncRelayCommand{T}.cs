using System.Windows.Input;

namespace QuantTrader.Commands
{
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Predicate<T?> _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public AsyncRelayCommand(Func<T, Task> execute)
        {
            ArgumentNullException.ThrowIfNull(execute);
            this._execute = execute;
        }

        public AsyncRelayCommand(Func<T, Task> execute, Predicate<T?> canExecute)
        {
            ArgumentNullException.ThrowIfNull(execute);
            ArgumentNullException.ThrowIfNull(canExecute);
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && _canExecute?.Invoke((T?)parameter) != false;
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            try
            {
                _isExecuting = true;
                if (parameter is T typedParameter)
                {
                    await _execute((T)parameter);
                }
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }
}
