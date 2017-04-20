using System;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Microsoft.IoT.Lightning.Providers;

namespace Iot.Common
{
    public class I2CManager
    {
        public string BusName { get; } = "I2C1";
        private readonly Logger _logger;
        private DeviceInformation _I2CBus;
        private I2cController _i2cController;

        public I2CManager(Logger logger)
        {
            _logger = logger;
        }

        public async Task Init()
        {
            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
                _i2cController = await I2cController.GetDefaultAsync();
            }
            else
            {
                _I2CBus = await FindI2CController(BusName);

            }
        }

        private async Task<DeviceInformation> FindI2CController(string busName)
        {
            var aqs = I2cDevice.GetDeviceSelector(busName);              /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);         /* Find the I2C bus controller device with our selector string           */
            if (dis.Count != 0) return dis[0];

            #pragma warning disable 4014
            _logger.LogException("No I2C controllers were found on the system", new Exception("Unexpected Error"));
            #pragma warning restore 4014
            return null;
        }
        public async Task<I2cDevice> FindI2CDevice(int slaveAddress, I2cBusSpeed busSpeed, I2cSharingMode sharingMode)
        {
            var settings = new I2cConnectionSettings(slaveAddress)
            {
                BusSpeed = busSpeed,
                SharingMode = sharingMode,
            };

            I2cDevice device;

            if (LightningProvider.IsLightningEnabled)
            {
                device = await Task<I2cDevice>.Factory.StartNew(() => _i2cController.GetDevice(settings));
            }
            else
            {
                device = await I2cDevice.FromIdAsync(_I2CBus.Id, settings);
            }

            if (device != null) return device;


            #pragma warning disable 4014
            _logger.LogInfo($"Slave address {settings.SlaveAddress} on I2C Controller is currently in use by " +
            #pragma warning restore 4014
                            "another application, or was not found. Please ensure that no other applications are using I2C and your device is correctly connected to the I2C bus.");
            return null;
        }
    }
}
