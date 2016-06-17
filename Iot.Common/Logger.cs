using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Common
{
    public class Logger : ILogger
    {
        private readonly Action<string> _logInfoMethod;
        private readonly Action<string, Exception> _logErrorMethod;

        public Logger(Action<string> logInfoMethod, Action<string,Exception> logErrorMethod)
        {
            _logInfoMethod = logInfoMethod;
            _logErrorMethod = logErrorMethod;
        }

        public async Task LogInfo(string message)
        {
            message = MessageFormatter(message);
            System.Diagnostics.Debug.WriteLine(message);
            await Task.Run(() => _logInfoMethod(message));
        }

        public async Task LogException(string message, Exception exception)
        {
            var formattedMessage = MessageFormatter(message, exception.Message);
            System.Diagnostics.Debug.WriteLine(formattedMessage);
            await Task.Run(() => _logErrorMethod(message, exception));
        }

        private string MessageFormatter(params string[] messageParts)
        {
            var strings = messageParts.ToArray();

            var sb = new StringBuilder();

            sb.Append(Now.ToString("R"));

            foreach (var messagePart in strings)
            {
                sb.Append("-");
                sb.Append(messagePart);
            }

            var output = sb.ToString();
            return output;
        }

        public DateTime Now => DateTime.Now;

    }
}