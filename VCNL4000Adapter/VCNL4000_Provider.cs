using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace VCNL4000Adapter
{
    // ReSharper disable InconsistentNaming
    public class VCNL4000_Provider
    {
        private I2cController controller;
        public VCNL4000_Provider()
        {
            // The code below should work the same with any provider, including Lightning and the default one.

            var awaiter = I2cController.GetDefaultAsync().GetAwaiter();
            controller = awaiter.GetResult();

            Task.Factory.StartNew(async () => controller = await I2cController.GetDefaultAsync());
        }
        public async Task<I2cDevice> GetSensorAsync()
        {
            I2cDevice sensor = null;
            await Task.Factory.StartNew<I2cDevice>(() => sensor = controller.GetDevice(new I2cConnectionSettings(VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode())));
            return sensor;
        }

        public I2cDevice GetSensor()
        {
            I2cDevice sensor = null;
            sensor = controller.GetDevice(new I2cConnectionSettings(VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode()));
            return sensor;
        }

    }
}
