using System;
using System.Threading.Tasks;
using Iot.Common;
using PCA9685PWMServoContoller;
using ArduinoBridge;

namespace SonarManager
{
    public sealed class SonarCoordinator : ICommonDevice
    {
        public event EventHandler<PositionalDistanceEventArgs> PositionFound;
        public event EventHandler<ExceptionEventArgs> SensorException;

        private readonly PCA9685ServoContoller _contoller;
        private readonly int _minServoSetting;
        private readonly int _maxServoSetting;

        private ProximtyEventArgs _lastReading;
        private bool _stopSweeping;
        private readonly double _arcUnitsPerDegree;
        private readonly int _stepSize;
        private readonly Func<bool> _haveReading;
        private int _currentPosition;

        public SonarCoordinator(PCA9685ServoContoller contoller)
        {
            _contoller = contoller;

            //sensor.ProximityReceived += (sender, args) => _lastReading = args;
            //sensor.SensorException += (sender, args) => SensorException?.Invoke(this, args); //Pass exceptions from Arduino "sensor" along.

            _minServoSetting = 250;
            _maxServoSetting = 800;

            _contoller.ResetDevice();
            _contoller.SetPwmUpdateRate(60);
            _contoller.SetPwm(PwmChannel.C0, 0, _minServoSetting);
            Task.Delay(500).Wait();

            _currentPosition = _minServoSetting;
            _arcUnitsPerDegree = (_maxServoSetting - _minServoSetting)/180d;
            _stepSize = (int)Math.Floor(_arcUnitsPerDegree*5);
            _haveReading = () => true; //_lastReading?.RawValue != default(double);
        }

        private void Sweep()
        {
            _currentPosition = GetMinimumReading();
            _currentPosition = ClockWiseSweep(_currentPosition);
            _currentPosition = GetMaximumReading();
            AntiClockWiseSweep(_currentPosition);
            GetMinimumReading();
        }

        private int AntiClockWiseSweep(int position)
        {
            while (position > _minServoSetting)
            {
                position = position - _stepSize;
                SetPositionAndGetReading(position);
                _lastReading = null;
            }
            return position;
        }

        private int ClockWiseSweep(int position)
        {
            while (position < _maxServoSetting)
            {
                position = position + _stepSize;
                SetPositionAndGetReading(position);
                _lastReading = null;
            }
            return position;
        }

        private int GetMaximumReading()
        {
            _currentPosition = _maxServoSetting;
            SetPositionAndGetReading(_currentPosition);
            _lastReading = null;
            return _currentPosition;
        }

        private int GetMinimumReading()
        {
            var position = _minServoSetting;
            SetPositionAndGetReading(position);
            _lastReading = null;
            return position;
        }

        private void SetPositionAndGetReading(int position)
        {
            try
            {
                _contoller.SetPwm(PwmChannel.C0, 0, position);
                Task.Delay(50).Wait();
                if(_haveReading())
                    PositionFound?.Invoke(this, new PositionalDistanceEventArgs { Angle = Angle(position), Proximity = _lastReading.Proximity, RawValue = _lastReading.RawValue });
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs { Exception = ex, Message = "An error occured setting Angle and getting distance." });
            }
        }

        private int Angle(double position)
        {
            var angle = (int)Math.Round((position - _minServoSetting) / _arcUnitsPerDegree, MidpointRounding.AwayFromZero);
            return angle;
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
