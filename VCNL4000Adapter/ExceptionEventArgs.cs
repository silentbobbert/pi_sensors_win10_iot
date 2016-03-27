using System;

namespace VCNL4000Adapter
{
    public class ExceptionEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
