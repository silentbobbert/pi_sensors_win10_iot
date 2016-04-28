namespace Iot.Common
{
    public interface IProximityEventArgs
    {
        double Proximity { get; set; }
        double RawValue { get; set; }
    }
}