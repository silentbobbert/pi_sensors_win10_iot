using System;
using System.Linq;
using System.Text;

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

        public void LogInfo(string message)
        {
            message = MessageFormatter(message);
            System.Diagnostics.Debug.WriteLine(message);
            _logInfoMethod(message);
        }

        public void LogException(string message, Exception exception)
        {
            var formattedMessage = MessageFormatter(message, exception.Message);
            System.Diagnostics.Debug.WriteLine(formattedMessage);
            _logErrorMethod(message, exception);
        }

        private string MessageFormatter(params string[] messageParts)
        {
            var strings = messageParts.ToArray();

            var sb = new StringBuilder();

            sb.Append(Now.ToString("R"));

            foreach (var messagePart in messageParts)
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