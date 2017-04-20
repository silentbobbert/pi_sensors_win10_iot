using System;
using System.Threading.Tasks;
using Iot.Common;
using PCA9685PWMServoContoller;
using SRF08Adapter;

namespace SonarManager
{
    public sealed class SonarCoordinator : ICommonDevice
    {
        public event EventHandler<PositionalDistanceEventArgs> PositionFound;
        public event EventHandler<SRF08ExceptionEventArgs> SensorException;

        private readonly PCA9685ServoContoller _contoller;
        private int _minServoSetting;
        private int _maxServoSetting;

        private double? _lastReading;
        private bool _stopSweeping;
        private double _arcUnitsPerDegree;
        private int _stepSize;
        
        private int _currentPosition;
        private Mode _mode = Mode.Incrementing;

        private enum Mode
        {
            Incrementing = 0,
            Decrementing = 1
        }

        public SonarCoordinator(PCA9685ServoContoller contoller, SRF08Device sensorDevice)
        {
            _contoller = contoller;

            sensorDevice.DistanceFound += (sender, args) =>
            {
                _lastReading = args.Proximity;
                Move();
            };
        

            sensorDevice.ExceptionOccured += (sender, args) =>
            {
                _lastReading = null;
                //Pass exceptions from SRF08 "sensor" along.
                SensorException?.Invoke(this, args);
            }; 

            SetupServoController();
        }

        private void SetupServoController()
        {
            _minServoSetting = 250;
            _maxServoSetting = 800;

            _contoller.ResetDevice();
            _contoller.SetPwmUpdateRate(60);
            _contoller.SetPwm(PwmChannel.C0, 0, _minServoSetting);
            Task.Delay(500).Wait();

            _currentPosition = _minServoSetting;
            _arcUnitsPerDegree = (_maxServoSetting - _minServoSetting) / 180d;
            _stepSize = (int) Math.Floor(_arcUnitsPerDegree * 5);
        }

        private void Move()
        {
            if (!_lastReading.HasValue || _stopSweeping) return;

            switch (_mode)
            {
                case Mode.Incrementing:
                    _currentPosition = Math.Min(_currentPosition + _stepSize, _maxServoSetting);
                    _mode = _currentPosition == _maxServoSetting ? Mode.Decrementing : Mode.Incrementing;
                    break;
                case Mode.Decrementing:
                    _currentPosition = Math.Max(_currentPosition - _stepSize, _minServoSetting);
                    _mode = _currentPosition == _minServoSetting ? Mode.Incrementing : Mode.Decrementing;
                    break;
            }

            _contoller.SetPwm(PwmChannel.C0, 0, _currentPosition);
            //Task.Delay(50).Wait(); Sonar is sending data no faster than every 75 ms
            PositionFound?.Invoke(this, new PositionalDistanceEventArgs { Angle = Angle(_currentPosition), Proximity = _lastReading.GetValueOrDefault(), RawValue = 0 });
        }

        private int Angle(double position)
        {
            var angle = (int)Math.Round((position - _minServoSetting) / _arcUnitsPerDegree, MidpointRounding.AwayFromZero);
            return angle;
        }

        public void Start()
        {
            _stopSweeping = false;
        }

        public void Dispose()
        {
            _stopSweeping = true;
            PositionFound = null;
            _lastReading = null;
            SensorException = null;
        }
    }
}
