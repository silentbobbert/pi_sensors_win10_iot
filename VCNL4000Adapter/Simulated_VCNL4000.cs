using System;
using Windows.System.Threading;

namespace VCNL4000Adapter
{
    public class SimulatedVcnl4000 : IVCNL4000Device
    {
        private ThreadPoolTimer _simulatorTimer;
        public event EventHandler<ProximtyEventArgs> ProximityReceived;
        public event EventHandler<AmbientLightEventArgs> AmbientLightReceived;
        public event EventHandler<ExceptionEventArgs> SensorException;

        public void Dispose()
        {
        }

        public void Start()
        {
            _simulatorTimer = ThreadPoolTimer.CreatePeriodicTimer(_simulatorTimer_Tick, TimeSpan.FromMilliseconds(1000));
        }

        private void _simulatorTimer_Tick(ThreadPoolTimer timer)
        {
            var random = new Random();
            ProximityReceived?.Invoke(this, new ProximtyEventArgs
            {
                RawValue = random.Next(2000, 2500)
            });

            AmbientLightReceived?.Invoke(this, new AmbientLightEventArgs
            {
                RawValue = random.Next(150, 500)
            });
        }
    }
}