﻿using System;
using Iot.Common;

namespace HC_SR04Adapter
{
    public class ProximtyEventArgs : EventArgs, IProximityEventArgs
    {
        public double Proximity { get; set; }
        public double RawValue { get; set; }
    }
}