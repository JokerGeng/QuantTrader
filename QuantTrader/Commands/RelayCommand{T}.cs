using System.Windows.Input;

namespace QuantTrader.Commands
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T?> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<T> execute)
        {
            ArgumentNullException.ThrowIfNull(execute);
            this._execute = execute;
        }

        public RelayCommand(Action<T> execute, Predicate<T?> canExecute)
        {
            ArgumentNullException.ThrowIfNull(execute);
            ArgumentNullException.ThrowIfNull(canExecute);
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is null && !typeof(T).IsValueType)
            {
                return false;
            }

            // Type safety check
            if (parameter is not null && parameter is not T)
            {
                return false;
            }

            if (parameter is T typedParameter)
            {
                return _canExecute?.Invoke(typedParameter) != false;
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
            }
        }
    }
}
