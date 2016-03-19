using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace VCNL4000Adapter
{
    // ReSharper disable InconsistentNaming
    public class VCNL4000_Device
    {
        private I2cController controller;
        public VCNL4000_Device()
        {
            // The code below should work the same with any provider, including Lightning and the default one.
            Task.Factory.StartNew(async () => controller = await I2cController.GetDefaultAsync());
        }
        public async Task<I2cDevice> GetProximitySensorAsync()
        {
            I2cDevice sensor = null;
            await Task.Factory.StartNew<I2cDevice>(() => sensor = controller.GetDevice(new I2cConnectionSettings(VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode())));
            return sensor;
        }
    }
}
