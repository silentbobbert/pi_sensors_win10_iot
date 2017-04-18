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
            _timer?.Cancel();
            _timer = null;
        }

        public void Start()
        {
            Task.Delay(100).Wait();
            _timer = ThreadPoolTimer.CreatePeriodicTimer(PollSensor, TimeSpan.FromMilliseconds(5));
        }

        private void PollSensor(ThreadPoolTimer timer)
        {
            if(_polling) return;

            _polling = true;

            var result = new byte[2];

            _device.Write(new byte[] { CMD, 0x51 });
            Task.Delay(70).Wait();

            _device.WriteRead(new byte[]{ 0x02},  result);

            var range = (result[0] << 8) + result[1];
            Debug.WriteLine($"Range: {range} cm");

            _polling = false;
        }

        public byte SoftwareVersion()
        {
            var buffer = new byte[1];
            _device.WriteRead(new[] { CMD }, buffer);

            return buffer[0];
        }
        
    }
}