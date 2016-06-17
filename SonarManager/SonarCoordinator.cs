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
        private bool _stopSweeping = false;
        private readonly double _arcUnitsPerDegree;
        private readonly int _stepSize;
        private readonly Func<bool> _haveReading;

        public SonarCoordinator(PCA9685ServoContoller contoller, ArduinoSensor sensor)
        {
            _contoller = contoller;
            _sensor = sensor;

            _sensor.ProximityReceived += (sender, args) => _lastReading = args;
            _sensor.SensorException += (sender, args) => SensorException?.Invoke(this, args); //Pass exceptions from Arduino "sensor" along.

            _minServoSetting = 250;
            _maxServoSetting = 800;

            _contoller.ResetDevice();
            _contoller.SetPwmUpdateRate(60);
            _contoller.SetPwm(PwmChannel.C0, 0, _minServoSetting);
            Task.Delay(1000).Wait();

            _arcUnitsPerDegree = (_maxServoSetting - _minServoSetting)/180d;
            _stepSize = (int)Math.Floor(_arcUnitsPerDegree*5);
            _haveReading = () => _lastReading?.RawValue != default(double);
        }

        private void Sweep()
        {
            var position = _minServoSetting;
            SetPositionAndGetReading(position);

            while (position < _maxServoSetting)
            {
                position = position + _stepSize;
                SetPositionAndGetReading(position);
                _lastReading = null;
            }

            SetPositionAndGetReading(_maxServoSetting);

            while (position > _minServoSetting)
            {
                position = position - _stepSize;
                SetPositionAndGetReading(position);
                _lastReading = null;

            }
            
        }

        private void SetPositionAndGetReading(int position)
        {
            try
            {
                _contoller.SetPwm(PwmChannel.C0, 0, position);
                Task.Delay(50).Wait();

                if (!_haveReading()) return;
                PositionFound?.Invoke(this, new PositionalDistanceEventArgs { Angle = position, Proximity = _lastReading.Proximity, RawValue = _lastReading.RawValue });
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs { Exception = ex, Message = "An error occured setting Angle and getting distance." });
            }
        }

        public void Start()
        {
            while (!_stopSweeping)
            {
                Sweep();
            }
            
        }

        public void Dispose()
        {
            _stopSweeping = true;
            PositionFound = null;
            SensorException = null;
        }
    }
}
