using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Iot.Common;

namespace pi_sensors_win10Core
{
    public class I2CDeviceLocator
    {
        private readonly ILogger _logger;
        private I2cDevice _device;

        public bool Ready => _device != null;
        public I2cDevice Device => _device;

        public I2CDeviceLocator(ILogger logger, string busName, int slaveAddres)
        {
            _logger = logger;
            InitI2CDevice(busName, slaveAddres);

            SpinWait.SpinUntil(() => Ready, TimeSpan.FromSeconds(300));
        }

        private async Task InitI2CDevice(string busName, int slaveAddres)
        {
            var aqs = I2cDevice.GetDeviceSelector(busName);                     /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);         /* Find the I2C bus controller device with our selector string           */
            if (dis.Count == 0)
            {
                _logger.LogException("No I2C controllers were found on the system", new Exception("Unexpected Error"));
                return;
            }

            var bus = dis[0];

            var settings = new I2cConnectionSettings(slaveAddres)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                SharingMode = I2cSharingMode.Shared,
            };
            _device = await I2cDevice.FromIdAsync(bus.Id, settings);

            if (_device != null) return;

            _logger.LogInfo($"Slave address {settings.SlaveAddress} on I2C Controller {bus.Id} is currently in use by " +
                            "another application, or was not found. Please ensure that no other applications are using I2C and your device is correctly connected to the I2C bus.");
        }
    }
}
