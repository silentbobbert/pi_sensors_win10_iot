﻿using System;
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
                var readBytes = new byte[2];
                _device.Read(readBytes);

                var pulseWidth = (ushort)(readBytes[0] << 8 | readBytes[1]);

                ProximityReceived?.Invoke(this, new ProximtyEventArgs
                {
                    RawValue = pulseWidth,
                    Proximity = Math.Max(0, ((pulseWidth/2d)/2.91d) - 0.5d) //The sensors are about 5mm inside the casing of the device.
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
