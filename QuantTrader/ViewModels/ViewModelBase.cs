using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QuantTrader.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 在UI线程上执行操作
        /// </summary>
        protected void ExecuteOnUI(Action action)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// 在UI线程上执行异步操作
        /// </summary>
        protected Task ExecuteOnUIAsync(Func<Task> asyncAction)
        {
            if (Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                return Application.Current.Dispatcher.InvokeAsync(asyncAction).Task;
            }
            else
            {
                return asyncAction();
            }
        }
    }
}
