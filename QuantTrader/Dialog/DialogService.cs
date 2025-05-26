using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace QuantTrader.Dialog
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResult?> ShowDialogAsync<TViewModel, TResult>()
            where TViewModel : IDialog<TResult>
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // 使用约定：视图类名 = ViewModel 名去掉 "Model"
            var viewTypeName = typeof(TViewModel).Name.Replace("ViewModel", "Window");
            var viewType = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.Name == viewTypeName && typeof(Window).IsAssignableFrom(t));

            if (viewType == null)
                throw new InvalidOperationException($"Cannot find view for {typeof(TViewModel).Name}");

            var window = (Window)Activator.CreateInstance(viewType)!;
            window.DataContext = viewModel;

            var task = viewModel.CloseRequest.Task;
            window.ShowDialog();

            return await task;
        }

        public async Task ShowDialogAsync<TViewModel>() where TViewModel : IDialog
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // 使用约定：视图类名 = ViewModel 名去掉 "Model"
            var viewTypeName = typeof(TViewModel).Name.Replace("ViewModel", "Window");
            var viewType = Assembly.GetExecutingAssembly().GetTypes()
                .FirstOrDefault(t => t.Name == viewTypeName && typeof(Window).IsAssignableFrom(t));

            if (viewType == null)
                throw new InvalidOperationException($"Cannot find view for {typeof(TViewModel).Name}");

            var window = (Window)Activator.CreateInstance(viewType)!;
            window.DataContext = viewModel;

            var task = viewModel.CloseRequest.Task;
            window.ShowDialog();

            await task;
        }
    }

}
