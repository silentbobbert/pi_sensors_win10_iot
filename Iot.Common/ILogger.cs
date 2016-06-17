using System;
using System.Threading.Tasks;

namespace Iot.Common
{
    public interface ILogger
    {
        Task LogInfo(string message);
        Task LogException(string message, Exception exception);
    }
}
