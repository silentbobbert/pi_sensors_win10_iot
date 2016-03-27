using System;

namespace VCNL4000Adapter
{
    public class AmbientLightEventArgs : EventArgs
    {
        //public decimal Proximity { get; set; }
        public int RawValue { get; set; }
    }
}