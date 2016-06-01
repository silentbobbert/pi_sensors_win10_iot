using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using ADS1115Adapter;
using ArduinoBridge;
using HeadlessRobot.DTOs;
using Iot.Common;
using Iot.Common.Utils;
using Sharp2Y0A21;
using VCNL4000Adapter;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace HeadlessRobot
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const double SharpConversionFactor = 4221057.491;
        private const double SharpExponent = 1.26814;


        private BackgroundTaskDeferral _deferral;
        private Dictionary<string, ICommonDevice> _devices;
        private Logger _logger;
        private Action<string> _logInfoAction;
        private Action<string, Exception> _logIErrorAction;
        private RawValueConverter _sharpSensorConverter;
        private bool _runTask = true;
        private readonly HttpClient _client;
        private readonly Uri _apiAddress = new Uri("https://10.21.9.149:44312/api/pilistener/message");

        public StartupTask()
        {
            _client = new HttpClient();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            SetupLogging();
            _logger = new Logger(_logInfoAction, _logIErrorAction);

            await _client.PostAsJsonAsync(_apiAddress, new SimpleDTO { Message = "Robot Starting!" });

            _sharpSensorConverter = new RawValueConverter(SharpConversionFactor, SharpExponent);

            _devices = new Dictionary<string, ICommonDevice>();

            await StartDevices();

            while (_runTask)
            {
                //run forever, or until cancelled!
            }

            await _client.PostAsJsonAsync(_apiAddress, new SimpleDTO { Message = "Robot Closing!" });
            _deferral.Complete();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _logger.LogInfo(reason.ToString());
            _runTask = false;
        }

        private void SetupLogging()
        {
            _logInfoAction = (message) =>
            {
                _client.PostAsJsonAsync(_apiAddress, new SimpleDTO {Message = message });
                Debug.WriteLine(message);
            };

            _logIErrorAction = (message, exception) =>
            {
                message = $"{message} - {exception.Message}";
                _client.PostAsJsonAsync(_apiAddress, new SimpleDTO { Message = message });
                Debug.WriteLine(message);
            };
        }

        private Task StartDevices()
        {
            IEnumerable<Task> devicesToStart = new[]
            {
                //InitSimulator(),
                InitI2cVCNL4000(),
                InitI2cADS1115(0x01),
                //InitArduinoI2C()
            };

            return Task.WhenAll(devicesToStart)
                .ContinueWith(all => _devices.ForEach(d =>
                    d.Value.Start()
                ));
        }
        private async Task InitSimulator()
        {
            // get the package architecure
            var package = Package.Current;
            var systemArchitecture = package.Id.Architecture.ToString();

            if (systemArchitecture.ToUpper().Contains("ARM")) return; //Dont simulate on ARM device - likely to be real device with real sensors!

            const string busName = "I2C1";

            IVCNL4000Device fakevcnl4000 = new SimulatedVcnl4000();
            fakevcnl4000.ProximityReceived += ProximityReceived_Handler;
            fakevcnl4000.AmbientLightReceived += Vcnl4000_AmbientLightReceived;
            fakevcnl4000.SensorException += SensorException_Handler;

            await Task.Run(() => _devices.Add(DeviceName(busName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode(), null), fakevcnl4000));
        }
        // ReSharper disable once InconsistentNaming
        private async Task InitI2cVCNL4000()
        {
            const string busName = "I2C1";
            var bus = await FindI2CController(busName);
            var device = await FindI2CDevice(bus, VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode());

            var initSettings = new VCNL4000Settings
            {
                IrCurrent_mA = 20,
                SensorTimeOut = 300,
                SensorPollInterval = 500,
                FirstPolynomial = 3.05101567E-11,
                SecondPolynomial = 3.75216617E-07,
                Dy = 4.98316469E-04
            };

            IVCNL4000Device vcnl4000 = new VCNL4000Device(device, initSettings);
            vcnl4000.ProximityReceived += ProximityReceived_Handler;
            vcnl4000.AmbientLightReceived += Vcnl4000_AmbientLightReceived;
            vcnl4000.SensorException += SensorException_Handler;

            _devices.Add(DeviceName(busName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode(), null), vcnl4000);
        }
        private async Task InitI2cADS1115(byte channel)
        {
            const string busName = "I2C1";
            var bus = await FindI2CController(busName);
            var device = await FindI2CDevice(bus, ADS1115_Constants.ADS1115_ADDRESS.GetHashCode());

            IADS1115Device ads1115 = new ADS1115Device(device, channel);
            ads1115.ChannelChanged += Ads1115ChannelChanged;
            _devices.Add(DeviceName(busName, (byte)ADS1115_Constants.ADS1115_ADDRESS.GetHashCode(), null), ads1115);
        }
        private async Task InitArduinoI2C()
        {
            const string busName = "I2C1";
            var bus = await FindI2CController(busName);
            var device = await FindI2CDevice(bus, 0x40);

            var arduino = new ArduinoSensor(device, 100);
            arduino.ProximityReceived += Sonar_ProximityReceived;
            arduino.SensorException += SensorException_Handler;

            _devices.Add(DeviceName(busName, 0x40, null), arduino);
        }
        private void Sonar_ProximityReceived(object sender, IProximityEventArgs e)
        {
            var message = $"Sonar Result Received {e.RawValue} Distance {e.Proximity} mm";
            _logger.LogInfo(message);
        }
        private async Task<DeviceInformation> FindI2CController(string busName)
        {
            var aqs = I2cDevice.GetDeviceSelector(busName);              /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);         /* Find the I2C bus controller device with our selector string           */
            if (dis.Count != 0) return dis[0];

            _logger.LogException("No I2C controllers were found on the system", new Exception("Unexpected Error"));
            return null;
        }
        private async Task<I2cDevice> FindI2CDevice(DeviceInformation bus, int slaveAddress)
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
        private void ProximityReceived_Handler(object sender, IProximityEventArgs e)
        {
            var message = $"Proximity Received - Raw Value {e.RawValue} & Approximate Distance in mm {e.Proximity:F3}";
            _logInfoAction(message);
        }
        private void Vcnl4000_AmbientLightReceived(object sender, AmbientLightEventArgs e)
        {
            var message = $"Ambient Light Received: {e.RawValue}";
            _logInfoAction(message);
        }
        private void SensorException_Handler(object sender, IExceptionEventArgs e)
        {
            var message = $"Error Received from Sensor : \"{e.Message} \"";
            _logIErrorAction($"Error Received from Sensor : \"{e.Message} \"", e.Exception);
        }
        private void Ads1115ChannelChanged(object sender, ChannelReadingDone e)
        {
            var convertedDistance = _sharpSensorConverter.Convert(e.RawValue);
            var message = $"Channel {e.Channel + 1} Message Received - Raw Value {e.RawValue} - Converted Distance {convertedDistance:F2} mm";
            _logInfoAction(message);

            
        }
        private string DeviceName(string busName, byte slaveAddress, string append)
        {

            var name = $"{busName}\\0x{slaveAddress:x8}";
            if (!string.IsNullOrEmpty(append))
            {
                name = $"{name}\\{append}";
            }
            return name;
        }
    }
}
