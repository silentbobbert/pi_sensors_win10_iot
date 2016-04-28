using System;

namespace Iot.Common
{
    public interface IExceptionEventArgs
    {
        string Message { get; set; }
        Exception Exception { get; set; }
    }
}