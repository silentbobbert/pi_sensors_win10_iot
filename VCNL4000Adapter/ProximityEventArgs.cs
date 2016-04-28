using System;
using Iot.Common;

namespace VCNL4000Adapter
{
    public class ProximtyEventArgs : EventArgs, IProximityEventArgs
    {
        public double Proximity { get; set; }
        public double RawValue { get; set; }
    }
}
