using System;
using Iot.Common;

namespace SRF08Adapter
{
    public class SRF08ExceptionEventArgs : IExceptionEventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}