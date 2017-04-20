using Iot.Common;

namespace SRF08Adapter
{
    public class SRF08DistanceFoundEventArgs : IProximityEventArgs
    {
        public double Proximity { get; set; }
        public double RawValue { get; set; }
    }
}