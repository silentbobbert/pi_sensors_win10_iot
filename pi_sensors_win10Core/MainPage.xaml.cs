using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ADS1115Adapter;
using ArduinoBridge;
using HC_SR04Adapter;
using Iot.Common;
using Iot.Common.Utils;
using Sharp2Y0A21;
using VCNL4000Adapter;

namespace pi_sensors_win10Core
{
    public sealed partial class MainPage : Page
    {
        private const double SharpConversionFactor = 4221057.491;
        private const double SharpExponent = 1.26814;


        // ReSharper disable once NotAccessedField.Local
        private readonly Logger _logger;
        private Dictionary<string, ICommonDevice> _devices;
        private Action<string> _logInfoAction;
        private Action<string, Exception> _logIErrorAction;
        private ThreadPoolTimer _simulatorTimer;
        private DateTime? _lastExceptionReceived;
        private RawValueConverter _sharpSensorConverter;

        private ThreadPoolTimer _uiCleanUp;

        public MainPage()
        {
            InitializeComponent();

            Unloaded += MainPage_Unloaded;

            SetupLogging();

            _lastExceptionReceived = Now.AddDays(-1);
            _uiCleanUp = ThreadPoolTimer.CreatePeriodicTimer(uiCleanUp_Tick, TimeSpan.FromSeconds(10));

            _logger = new Logger(_logInfoAction, _logIErrorAction);

            _devices = new Dictionary<string, ICommonDevice>();
            
            _sharpSensorConverter = new RawValueConverter(SharpConversionFactor, SharpExponent);

            StartDevices();
        }

        private void StartDevices()
        {
            IEnumerable<Task> devicesToStart = new[]
            {
                //InitI2cVCNL4000(),
                //InitI2cADS1115(0x01),
                InitArduinoI2C()
            };

            Task.WhenAll(devicesToStart)
                .ContinueWith(all => _devices.ForEach(d =>
                    d.Value.Start()
                    ));
        }

        private async Task InitSonarSensor()
        {
            var controller = await FindGPIOController();
            var sensor = new SonarSensor(gpioController: controller, trigPinNo: 16, echoPinNo: 18);
            sensor.ProximityReceived += Sensor_ProximityReceived;
            _devices.Add("Sonar Sensor", sensor);
        }

        private void Sensor_ProximityReceived(object sender, IProximityEventArgs e)
        {
            var message = $"Sonar Result Received {e.RawValue} Distance {e.Proximity} mm";
            UpdateUIAsync(() => sonarMessages.Text = message).Wait();
        }

        private void SetupLogging()
        {
            _logInfoAction = (message) => { Debug.WriteLine(message); };

            _logIErrorAction = (message, exception) =>
            {
                message = $"{message} - {exception.Message}";
                Debug.WriteLine(message);
            };
        }

        private void uiCleanUp_Tick(ThreadPoolTimer timer)
        {
            if (Now.Subtract(_lastExceptionReceived.GetValueOrDefault()).TotalMilliseconds > 2900)
            {
                UpdateErrorMessageBox("No Recent Exceptions Received");
            }
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

        private async Task UpdateProximityMessageBox(string message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        proximityMessages.Text = message;
                    });
        }
        private async Task UpdateAmbientMessageBox(string message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        ambientMessages.Text = message;
                    });
        }
        private async Task UpdateErrorMessageBox(string message)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        errorMessages.Text = message;
                    });
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

        private async Task InitArduinoI2C()
        {
            const string busName = "I2C1";
            var bus = await FindI2CController(busName);
            var device = await FindI2CDevice(bus, 0x40);

            var arduino = new ArduinoSensor(device);
            arduino.ProximityReceived += ProximityReceived_Handler;
            arduino.SensorException += SensorException_Handler;

            _devices.Add(DeviceName(busName, 0x40, null), arduino);
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

        private void Ads1115ChannelChanged(object sender, ChannelReadingDone e)
        {
            var convertedDistance = _sharpSensorConverter.Convert(e.RawValue);
            var message = $"Channel {e.Channel + 1} Message Received - Raw Value {e.RawValue} - Converted Distance {convertedDistance:F2} mm";
            _logInfoAction(message);

            switch (e.Channel)
            {
                case 0:
                    UpdateUIAsync(() => channelOneMessages.Text = message).Wait();
                    break;
                case 1:
                    UpdateUIAsync(() => channelTwoMessages.Text = message).Wait();
                    break;
                case 2:
                    UpdateUIAsync(() => channelThreeMessages.Text = message).Wait();
                    break;
                case 3:
                    UpdateUIAsync(() => channelFourMessages.Text = message).Wait();
                    break;
            }
        }
        private async Task UpdateUIAsync(Action updateCallback)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => updateCallback());
        }

        private void Vcnl4000_AmbientLightReceived(object sender, AmbientLightEventArgs e)
        {
            var message = $"Ambient Light Received: {e.RawValue}";
            UpdateAmbientMessageBox(message);
            _logInfoAction(message);
        }
        private void ProximityReceived_Handler(object sender, IProximityEventArgs e)
        {
            var message = $"Proximity Received - Raw Value {e.RawValue} & Approximate Distance in mm {e.Proximity:F3}";
            UpdateProximityMessageBox(message);
            _logInfoAction(message);
        }
        private void SensorException_Handler(object sender, IExceptionEventArgs e)
        {
            _lastExceptionReceived = Now;
            var message = $"Error Received from Sensor : \"{e.Message} \"";
            UpdateErrorMessageBox(message);
            _logIErrorAction($"Error Received from Sensor : \"{e.Message} \"", e.Exception);
        }

        public DateTime Now => DateTime.Now;

        private string DeviceName(string busName, byte slaveAddress, string append)
        {
            
            var name = $"{busName}\\0x{slaveAddress:x8}";
            if (!string.IsNullOrEmpty(append))
            {
                name = $"{name}\\{append}";
            }
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

        private async Task<GpioController> FindGPIOController()
        {
            var gpio = await GpioController.GetDefaultAsync();
            if (gpio == null)
            {
                UpdateErrorMessageBox("There is no GPIO controller on this device");
            }
            return gpio;
        }
    }
}
