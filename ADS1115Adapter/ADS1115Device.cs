using Windows.Devices.I2c;

namespace ADS1115Adapter
{
    public class ADS1115Device : IADS1115Device
    {
        private I2cDevice ads1115;
        public ADS1115Device(I2cDevice device)
        {
            ads1115 = device;
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }
    }
}