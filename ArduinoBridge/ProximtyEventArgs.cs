using System;
using Iot.Common;

namespace ArduinoBridge
{
    public class ProximtyEventArgs : EventArgs, IProximityEventArgs
    {
        public double Proximity { get; set; }
        public double RawValue { get; set; }
    }
}