using System;
using System.Collections.Generic;
using System.Text;

namespace Chiats.TinyPass
{
    public interface IColumnValueConvert
    {
        object GetValue(object Value);
    }
}
