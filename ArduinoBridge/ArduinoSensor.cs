using System;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Iot.Common;

namespace ArduinoBridge
{
    public sealed class ArduinoSensor : ICommonI2CDevice
    {
        public event EventHandler<ProximtyEventArgs> ProximityReceived;
        public event EventHandler<ExceptionEventArgs> SensorException;

        private readonly I2cDevice _device;
        private ThreadPoolTimer _arduinoTimer;
        private readonly double _sensorPollInterval;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device">The Arduino device</param>
        /// <param name="interval">Poll Interval in ms</param>
        public ArduinoSensor(I2cDevice device, double interval)
        {
            _device = device;
            _sensorPollInterval = interval;
        }
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
                var arduinoBytes = new byte[10];
                _device.Read(arduinoBytes);

                var sonarWidth = (ushort)(arduinoBytes[0] << 8 | arduinoBytes[1]);

                //var adcReading1 = (ushort)(arduinoBytes[2] << 8 | arduinoBytes[3]);
                //var adcReading2 = (ushort)(arduinoBytes[4] << 8 | arduinoBytes[5]);
                //var adcReading3 = (ushort)(arduinoBytes[6] << 8 | arduinoBytes[7]);
                //var adcReading4 = (ushort)(arduinoBytes[8] << 8 | arduinoBytes[9]);

                ProximityReceived?.Invoke(this, new ProximtyEventArgs
                {
                    RawValue = sonarWidth,
                    Proximity = Math.Max(0, ((sonarWidth / 2d)/2.91d) - 0.5d) //The sensors are about 5mm inside the casing of the device.
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
    }
}
