using System;

namespace Iot.Common
{
    public interface ICommonI2CDevice : IDisposable
    {
        void Start();
    }
}