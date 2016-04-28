using System;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Iot.Common;

namespace ArduinoBridge
{
    public class ArduinoSensor : ICommonI2CDevice
    {
        public event EventHandler<ProximtyEventArgs> ProximityReceived;
        public event EventHandler<ExceptionEventArgs> SensorException;

        private readonly I2cDevice _device;
        private ThreadPoolTimer _arduinoTimer;
        private readonly double _sensorPollInterval;

        public void Dispose()
        {
            _arduinoTimer.Cancel();
            _arduinoTimer = null;
        }

        public void Start()
        {
            _arduinoTimer = ThreadPoolTimer.CreatePeriodicTimer(arduino_TimerTick, TimeSpan.FromMilliseconds(_sensorPollInterval));
        }

        private void arduino_TimerTick(ThreadPoolTimer timer)
        {
            GetValue();
        }

        private void GetValue()
        {
            try
            {
                var readBytes = new byte[2];
                _device.Read(readBytes);

                var duration = readBytes[1] << 8 | readBytes[0];

                ProximityReceived?.Invoke(this, new ProximtyEventArgs
                {
                    RawValue = duration,
                    Proximity = (duration/2d)/29.1d
                });
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An error occurred reading values from Arduino Sensor Device"
                });
            }
        }

        public ArduinoSensor(I2cDevice device)
        {
            _device = device;
            _sensorPollInterval = 500;
        }
    }
}
