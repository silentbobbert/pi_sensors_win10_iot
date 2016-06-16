using System;
using System.Threading;
using System.Threading.Tasks;
using ArduinoBridge;
using Iot.Common;
using PCA9685PWMServoContoller;

namespace SonarManager
{
    public sealed class SonarCoordinator : ICommonDevice
    {
        public event EventHandler<PositionalDistanceEventArgs> PositionFound;
        public event EventHandler<ExceptionEventArgs> SensorException;

        private readonly PCA9685ServoContoller _contoller;
        private readonly ArduinoSensor _sensor;
        private readonly int _minServoSetting;
        private readonly int _maxServoSetting;

        private ProximtyEventArgs _lastReading;

        public SonarCoordinator(PCA9685ServoContoller contoller, ArduinoSensor sensor)
        {
            _contoller = contoller;
            _sensor = sensor;

            _sensor.ProximityReceived += _sensor_ProximityReceived;

            _minServoSetting = 250;
            _maxServoSetting = 800;

            _contoller.SetPwmUpdateRate(60);
            _contoller.SetPwm(PwmChannel.C0, 0, _minServoSetting);
            Task.Delay(1000).Wait();

        }

        private void _sensor_ProximityReceived(object sender, ProximtyEventArgs e)
        {
            _lastReading = e;
        }

        private void Sweep()
        {
            var arcUnitsPerDegree = (_maxServoSetting - _minServoSetting)/180d; //3.05 degrees per unit.
            var stepSize = (int)Math.Floor(arcUnitsPerDegree*5); //10 Degrees

            Func<bool> haveReading = () => _lastReading != null;

            for (var position = _minServoSetting; position < _maxServoSetting;)
            {
                position = position + stepSize;
                haveReading = SetPositionAndGetReading(position, haveReading);
            }

            for (var position = _maxServoSetting; position > _minServoSetting;)
            {
                position = position - stepSize;
                haveReading = SetPositionAndGetReading(position, haveReading);
            }
        }

        private Func<bool> SetPositionAndGetReading(int position, Func<bool> haveReading)
        {
            try
            {
                Task.Delay(100).Wait();
                //SpinWait.SpinUntil(haveReading);
                _contoller.SetPwm(PwmChannel.C0, 0, position);
                PositionFound?.Invoke(this, new PositionalDistanceEventArgs {Angle = position, Proximity = _lastReading.Proximity});
                return null;
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs { Exception = ex, Message = "An error occured setting Angle and getting distance." });
            }
            return null;
        }

        public void Start()
        {
            while (true)
            {
                Sweep();
            }
            
        }

        public void Dispose()
        {
            PositionFound = null;
            SensorException = null;
        }
    }
}
