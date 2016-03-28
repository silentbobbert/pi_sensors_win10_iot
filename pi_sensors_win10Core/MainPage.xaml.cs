using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Windows.UI.Core;
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
        private ThreadPoolTimer _simulatorTimer;
        private DateTime? _lastExceptionReceived;

        private ThreadPoolTimer _uiCleanUp;

        public MainPage()
        {
            InitializeComponent();

            Unloaded += MainPage_Unloaded;

            _logInfoAction = (message) =>
            {
                Debug.WriteLine(message);
            };

            _logIErrorAction = (message, exception) =>
            {
                message = $"{message} - {exception.Message}";
                Debug.WriteLine(message);
            };

            _lastExceptionReceived = Now.AddDays(-1);
            _uiCleanUp = ThreadPoolTimer.CreatePeriodicTimer(uiCleanUp_Tick, TimeSpan.FromSeconds(10));

            _logger = new Logger(_logInfoAction, _logIErrorAction);

            _devices = new Dictionary<string, ICommonI2CDevice>();

            Task.Factory.StartNew(async () => await InitI2cVCNL4000()
                .ContinueWith(async t => await InitSimulator())
                .ContinueWith(t => 
                    _devices.ForEach(d => d.Value.Start())
            ));
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

            await Task.Run(() => _devices.Add(DeviceName(busName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode()), fakevcnl4000));
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
            var bus = await FindController(busName);
            var device = await FindDevice(bus, VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode());

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

            _devices.Add(DeviceName(busName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode()), vcnl4000);
        }
        private void Vcnl4000_AmbientLightReceived(object sender, AmbientLightEventArgs e)
        {
            var message = $"Ambient Light Received: {e.RawValue}";
            UpdateAmbientMessageBox(message);
            _logInfoAction(message);
        }
        private void ProximityReceived_Handler(object sender, ProximtyEventArgs e)
        {
            var message = $"Proximity Received - Raw Value {e.RawValue} & Approximate Distance in mm {e.Proximity:F3}";
            UpdateProximityMessageBox(message);
            _logInfoAction(message);
        }
        private void SensorException_Handler(object sender, ExceptionEventArgs e)
        {
            _lastExceptionReceived = Now;
            var message = $"Error Received from Sensor : \"{e.Message} \"";
            UpdateErrorMessageBox(message);
            _logIErrorAction($"Error Received from Sensor : \"{e.Message} \"", e.Exception);
        }

        public DateTime Now => DateTime.Now;

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
