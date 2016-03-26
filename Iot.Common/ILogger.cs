using System;

namespace Iot.Common
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogException(string message, Exception exception);
    }
}
