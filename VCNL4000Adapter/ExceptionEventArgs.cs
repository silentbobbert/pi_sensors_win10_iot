﻿using System;
using Iot.Common;

namespace VCNL4000Adapter
{
    public class ExceptionEventArgs : EventArgs, IExceptionEventArgs
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }
}
