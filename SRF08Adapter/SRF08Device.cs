using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Iot.Common;

namespace SRF08Adapter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SRF08Device : ICommonDevice
    {
        public event EventHandler<SRF08DistanceFoundEventArgs> DistanceFound;
        public event EventHandler<SRF08ExceptionEventArgs> ExceptionOccured;

        private readonly I2cDevice _device;
        private ThreadPoolTimer _timer;
        private bool _polling;

        private byte CMD = 0x00;

        public SRF08Device(I2cDevice device)
        {
            _device = device;
        }

        public void Dispose()
        {
            DistanceFound = null;
            ExceptionOccured = null;
            _timer?.Cancel();
            _timer = null;
        }

        public void Start()
        {
            try
            {
                Task.Delay(100).Wait();
                _timer = ThreadPoolTimer.CreatePeriodicTimer(PollSensor, TimeSpan.FromMilliseconds(5));
            }
            catch (Exception e)
            {
                ExceptionOccured?.Invoke(this, new SRF08ExceptionEventArgs {Exception = e, Message = "Error starting sonar sensor"});
            }
        }

        private void PollSensor(ThreadPoolTimer timer)
        {
            if(_polling) return;

            try
            {
                _polling = true;

                var result = new byte[2];

                _device.Write(new byte[] { CMD, 0x51 });
                Task.Delay(70).Wait();

                _device.WriteRead(new byte[] { 0x02 }, result);

                var range = (result[0] << 8) + result[1];
                Debug.WriteLine($"Range: {range} cm");

                DistanceFound?.Invoke(this, new SRF08DistanceFoundEventArgs { Proximity = range });
                _polling = false;

            }
            catch (Exception e)
            {
                ExceptionOccured?.Invoke(this, new SRF08ExceptionEventArgs {Exception = e, Message = "Error occured getting Range"});
            }
        }

        public byte SoftwareVersion()
        {
            var buffer = new byte[1];
            _device.WriteRead(new[] { CMD }, buffer);

            return buffer[0];
        }
        
    }
}