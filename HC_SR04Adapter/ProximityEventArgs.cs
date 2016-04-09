using System;

namespace HC_SR04Adapter
{
    public class ProximtyEventArgs : EventArgs
    {
        //public double Proximity { get; set; }
        public int RawValue { get; set; }
    }
}