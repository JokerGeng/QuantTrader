using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Dialog
{
    public interface IDialog<TResult>
    {
        TaskCompletionSource<TResult?> CloseRequest { get; }
    }

    public interface IDialog
    {
        TaskCompletionSource CloseRequest { get; }
    }
}
