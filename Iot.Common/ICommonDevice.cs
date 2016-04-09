using System;

namespace Iot.Common
{
    public interface ICommonDevice : IDisposable
    {
        void Start();
    }
}