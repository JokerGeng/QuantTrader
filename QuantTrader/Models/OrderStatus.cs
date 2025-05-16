using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantTrader.Models
{
    public enum OrderStatus
    {
        Created,
        Submitted,
        PartiallyFilled,
        Filled,
        Canceled,
        Rejected,
        Unknown
    }
}
