using System;

namespace HC_SR04Adapter
{
    public class ProximtyEventArgs : EventArgs
    {
        public double Distance { get; set; }
        public double RawValue { get; set; }
    }
}