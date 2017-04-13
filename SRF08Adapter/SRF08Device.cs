using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Windows.System.Threading;

namespace SRF08Adapter
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SRF08Device : ISRF08Device
    {
        private readonly I2cDevice _device;
        private ThreadPoolTimer _timer;

        private const byte CMD = 0x00;                             // Command byte, values of 0 being sent with write have to be masked as a byte to stop them being misinterpreted as NULL this is a bug with arduino 1.0
        private const byte LIGHTBYTE = 0x01;                                 // Byte to read light sensor
        private const byte RANGEBYTE = 0x02;                                // Byte for start of ranging data
        private const byte BeginRangingInCm = 0x51;

        public SRF08Device(I2cDevice device)
        {
            _device = device;
        }

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public void Start()
        {
            PollSensor(null);
            _timer = ThreadPoolTimer.CreatePeriodicTimer(PollSensor, TimeSpan.FromMilliseconds(30000));
        }

        private void PollSensor(ThreadPoolTimer timer)
        {
            var range = GetRange();
            Debug.WriteLine($"Range: {range}cm");
        }

        private int GetRange()
        {
            var readBuffer = new byte[4];
            var range = 0;

            _device.WriteRead(,readBuffer);

            var rangeCommand = new[] { BeginRangingInCm };

            _device.Write(rangeCommand);
            Task.Delay(100); // Wait for ranging to be complete

            _device.Write(new []{RANGEBYTE});

            _device.Read(readBuffer);


            var highByte = readBuffer[2];                  
            var lowByte = readBuffer[3];

            range = (highByte << 8) + lowByte;             

            return range;                                  
        }
    }
}