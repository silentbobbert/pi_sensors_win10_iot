using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Iot.Common;
using Iot.Common.Utils;
using VCNL4000Adapter;

namespace pi_sensors_win10Core
{
    public sealed partial class MainPage : Page
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly Logger _logger;
        private Dictionary<string, ICommonI2CDevice> _devices;
        private Action<string> _logInfoAction;
        private Action<string, Exception> _logIErrorAction;

        public MainPage()
        {
            InitializeComponent();

            Unloaded += MainPage_Unloaded;

            _logInfoAction = (message) =>
            {
                //statusMessages.Text = message;
                Debug.WriteLine(message);
            };

            _logIErrorAction = (message, exception) =>
            {
                //statusMessages.Text = $"{message} - {exception.Message}";
                Debug.WriteLine($"{message} - {exception.Message}");
            };

            _logger = new Logger(_logInfoAction, _logIErrorAction);

            _devices = new Dictionary<string, ICommonI2CDevice>();

            Task.Factory.StartNew(async () => await InitI2cVCNL4000()
                .ContinueWith(t => 
                    _devices.ForEach(d => d.Value.Start())
            ));
        }
        // ReSharper disable once InconsistentNaming
        private async Task InitI2cVCNL4000()
        {
            const string busName = "I2C1";
            var bus = await FindController(busName);
            var device = await FindDevice(bus, VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode());

            IVCNL4000Device vcnl4000 = new VCNL4000Device(device, 20);
            vcnl4000.ProximityReceived += ProximityReceived_Handler;
            vcnl4000.AmbientLightReceived += Vcnl4000_AmbientLightReceived;
            vcnl4000.SensorException += SensorException_Handler;

            _devices.Add(DeviceName(busName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode()), vcnl4000);
        }
        private void Vcnl4000_AmbientLightReceived(object sender, AmbientLightEventArgs e)
        {
            _logInfoAction($"Ambient Light Received: {e.RawValue}");
        }
        private void ProximityReceived_Handler(object sender, ProximtyEventArgs e)
        {
            _logInfoAction($"Proximity Received: {e.RawValue}");
        }
        private void SensorException_Handler(object sender, ExceptionEventArgs e)
        {
            _logIErrorAction($"Error Received from Sensor : \"{e.Message} \"", e.Exception);
        }
        private string DeviceName(string busName, byte slaveAddress)
        {
            
            var name = $"{busName}\\0x{slaveAddress:x8}";
            return name;
        }
        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _devices.ForEach(d => d.Value.Dispose());
        }
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            _devices.ForEach(d => d.Value.Dispose());
            Application.Current.Exit();
        }
        private async Task<DeviceInformation> FindController(string busName)
        {
            var aqs = I2cDevice.GetDeviceSelector(busName);                     /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);         /* Find the I2C bus controller device with our selector string           */
            if (dis.Count != 0) return dis[0];

            _logger.LogException("No I2C controllers were found on the system", new Exception("Unexpected Error"));
            return null;
        }
        private async Task<I2cDevice> FindDevice(DeviceInformation bus, int slaveAddress)
        {
            var settings = new I2cConnectionSettings(slaveAddress)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                SharingMode = I2cSharingMode.Shared,
            };
            var device = await I2cDevice.FromIdAsync(bus.Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
            if (device != null) return device;


            _logger.LogInfo($"Slave address {settings.SlaveAddress} on I2C Controller {bus.Id} is currently in use by " +
                            "another application, or was not found. Please ensure that no other applications are using I2C and your device is correctly connected to the I2C bus.");
            return null;
        }
    }
}
