﻿using System;

namespace VCNL4000Adapter
{
    public class ProximtyEventArgs : EventArgs
    {
        public double Proximity { get; set; }
        public int RawValue { get; set; }
    }
}
