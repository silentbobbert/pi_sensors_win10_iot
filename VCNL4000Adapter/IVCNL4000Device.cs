using System;
using Iot.Common;

namespace VCNL4000Adapter
{
    public interface IVCNL4000Device : ICommonI2CDevice
    {
        event EventHandler<ProximtyEventArgs> ProximityReceived;
        event EventHandler<AmbientLightEventArgs> AmbientLightReceived;
        event EventHandler<ExceptionEventArgs> SensorException;
    }
}