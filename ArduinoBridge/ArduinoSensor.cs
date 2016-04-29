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

                var pulseWidth = (ushort)(readBytes[0] << 8 | readBytes[1]);

                ProximityReceived?.Invoke(this, new ProximtyEventArgs
                {
                    RawValue = pulseWidth,
                    Proximity = (pulseWidth/2d)/2.91d
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

        public ArduinoSensor(I2cDevice device, double interval)
        {
            _device = device;
            _sensorPollInterval = interval;
        }
    }
}
