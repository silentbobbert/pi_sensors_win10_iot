using System;
using System.IO;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Iot.Common;

namespace HC_SR04Adapter
{
    public class SonarSensor : ICommonDevice
    {
        private readonly int _echoPinNo;
        private readonly GpioController _gpioController;
        private readonly int _trigPinNo;

        private GpioPin _trig;
        private GpioPin _echo;

        private DispatcherTimer _timer;

        public event EventHandler<ProximtyEventArgs> ProximityReceived;

        public SonarSensor(GpioController gpioController, int trigPinNo, int echoPinNo)
        {
            _gpioController = gpioController;
            _trigPinNo = trigPinNo;
            _echoPinNo = echoPinNo;

            Init();

        }

        private void Init()
        {
            GpioOpenStatus trigStatus;
            GpioOpenStatus echoStatus;

            _gpioController.TryOpenPin(_trigPinNo, GpioSharingMode.Exclusive, out _trig, out trigStatus);
            _gpioController.TryOpenPin(_echoPinNo, GpioSharingMode.Exclusive, out _echo, out echoStatus);

            if (trigStatus == GpioOpenStatus.PinOpened && echoStatus == GpioOpenStatus.PinOpened) return;

            throw new IOException("Count not get exclusive access to the required pins");
        }

        public void Start()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(250);
            _timer.Tick += _timer_Tick;
            
        }

        private void _timer_Tick(object sender, object e)
        {
            ProximityReceived?.Invoke(this, new ProximtyEventArgs { RawValue = 100 });
        }

        public void Dispose()
        {
            _trig.Dispose();
            _echo.Dispose();

            _timer?.Stop();
            _timer = null;
        }
    }
}
