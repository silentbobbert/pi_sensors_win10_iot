using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.System.Threading;
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

        private ThreadPoolTimer _timer;
        private Stopwatch _sw;

        private static bool _readingTakingPlace = false;

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

            _trig.SetDriveMode(GpioPinDriveMode.Input);
            _echo.SetDriveMode(GpioPinDriveMode.Output);

            _trig.Write(GpioPinValue.Low);
            _sw = new Stopwatch();

            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));

            if (trigStatus == GpioOpenStatus.PinOpened && echoStatus == GpioOpenStatus.PinOpened) return;

            throw new IOException("Count not get exclusive access to the required pins");
        }
        private void timer_Tick(ThreadPoolTimer timer)
        {
            if (_readingTakingPlace) return;

            _readingTakingPlace = true;
            var raw = GetEchoReading();
            ProximityReceived?.Invoke(this, new ProximtyEventArgs { RawValue = raw, Proximity = CalculateDistance(raw) });
            _readingTakingPlace = false;
        }

        private double CalculateDistance(double time_ms)
        {
            //Speed of sound = 340.29 m/s ==> 340,290 mm/s
            //10 million ticks in a second
            return 170145*(time_ms/1000d);
        }
        private double GetEchoReading()
        {
            //1 Tick = 0.1 microseconds
            //10 tick = 1 microseconds
            //100 tick = 10 microseconds
            //1000 tick = 100 microseconds
            //10,000 tick = 1000 microseconds == 1 millisecond

            _trig.Write(GpioPinValue.High);
            SpinWait.SpinUntil(() => false, TimeSpan.FromTicks(100));
            _trig.Write(GpioPinValue.Low);

            while (_echo.Read() == GpioPinValue.Low)
            {
                //hold here
            }
            _sw.Start();

            while (_echo.Read() == GpioPinValue.High)
            {
                //hold here
            }
            _sw.Stop();
            
            return TimeSpan.FromTicks(_sw.ElapsedTicks).TotalMilliseconds;
        }

        public void Start()
        {
            _timer = ThreadPoolTimer.CreatePeriodicTimer(timer_Tick, TimeSpan.FromMilliseconds(250));
        }

        public void Dispose()
        {
            _trig.Dispose();
            _echo.Dispose();
            _timer.Cancel();
            _timer = null;
        }
    }
}
