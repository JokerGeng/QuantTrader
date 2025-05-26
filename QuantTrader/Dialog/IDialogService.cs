using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Dialog
{
    public interface IDialogService
    {   /// <summary>
        /// 显示对话框并获取结果
        /// </summary>
        /// <typeparam name="TViewModel">窗口绑定的 ViewModel</typeparam>
        /// <typeparam name="TResult">返回的结果</typeparam>
        Task<TResult?> ShowDialogAsync<TViewModel,TResult>()
            where TViewModel : IDialog<TResult>;

        Task ShowDialogAsync<TViewModel>()
         where TViewModel : IDialog;
    }
}
