using System;
using Iot.Common;

namespace ArduinoBridge
{
    public class ExceptionEventArgs : EventArgs, IExceptionEventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}