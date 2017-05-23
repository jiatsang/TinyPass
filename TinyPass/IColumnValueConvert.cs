using System;
using System.Collections.Generic;
using System.Text;

namespace Chiats.nTinyPass
{
    public interface IColumnValueConvert
    {
        object GetValue(object Value);
    }
}
