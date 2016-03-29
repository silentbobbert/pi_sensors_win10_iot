using System;
using Iot.Common;

namespace ADS1115Adapter
{
    public interface IADS1115Device : ICommonI2CDevice
    {
        event EventHandler<ChannelOneReadingDone> ChannelOneReady;
    }
}