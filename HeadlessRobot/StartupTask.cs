using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Storage.Streams;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using ADS1115Adapter;
using ArduinoBridge;
using HeadlessRobot.DTOs;
using Iot.Common;
using Iot.Common.Utils;
using Newtonsoft.Json;
using PCA9685PWMServoContoller;
using Sharp2Y0A21;
using VCNL4000Adapter;
using SonarManager;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace HeadlessRobot
{
    public sealed class StartupTask : IBackgroundTask
    {
        private const double SharpConversionFactor = 4221057.491;
        private const double SharpExponent = 1.26814;
        private const int ArduinoSlaveAddress = 0x07;
        private const int ServoControllerAddress = 0x40;
        private const string BusName = "I2C1";

        private BackgroundTaskDeferral _deferral;
        private readonly Dictionary<string, ICommonDevice> _devices;
        private Logger _logger;
        private Action<string> _logInfoAction;
        private Action<string, Exception> _logIErrorAction;
        private readonly RawValueConverter _sharpSensorConverter;
        private bool _runTask = true;
        private readonly Uri _apiAddress = new Uri("https://10.21.9.149/RemoteService/api/pilistener/message");
        private static readonly DataChanged MessageObject = new DataChanged();
        private readonly object _lock = new object();
        private HttpClient _client;

        private readonly ObservableCollection<DeviceInformation> _listOfDevices;
        private readonly CancellationTokenSource _readCancellationTokenSource = new CancellationTokenSource();
        private SerialDevice _serialPort = null;
        private DataReader _dataReaderObject = null;
        private DeviceInformation _I2CBus;

        public StartupTask()
        {
            //_readCancellationTokenSource = new CancellationTokenSource();
            _listOfDevices = new ObservableCollection<DeviceInformation>();
            SetupLogging();
            _sharpSensorConverter = new RawValueConverter(SharpConversionFactor, SharpExponent);
            _devices = new Dictionary<string, ICommonDevice>();

            SetupAPIClient();
        }
        // ReSharper disable once InconsistentNaming
        private void SetupAPIClient()
        {
            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Expired);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            _client = new HttpClient(filter);
        }

        public IAsyncAction PostMessageToApiAction()
        {
            return PostMessageToAPI().AsAsyncAction();
        }
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private async Task PostMessageToAPI()
        {
            try
            {
                var stringContent = new HttpStringContent(JsonConvert.SerializeObject(MessageObject), UnicodeEncoding.Utf8, "application/json");

                var result = await _client.PostAsync(_apiAddress, stringContent);
                Debug.WriteLine($"Posting Message result was successful? : {result.IsSuccessStatusCode} Message sent was {stringContent.ToString()}");
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            _I2CBus = await FindI2CController(BusName);

            //await ListAvailablePorts(); //Does not work on RPi3 - yet. Should work on RPi2...

            //await SetSerialDevice();
            //await Listen();

            await StartDevices();

            

            while (_runTask)
            {
                //run forever, or until cancelled!
            }

            _deferral.Complete();
        }

        private async Task SetSerialDevice()
        {
            try
            {
                _serialPort = await SerialDevice.FromIdAsync(_listOfDevices.First().Id);

                // ...

                // Configure serial settings
                _serialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                _serialPort.BaudRate = 19200;
                _serialPort.Parity = SerialParity.None;
                _serialPort.StopBits = SerialStopBitCount.One;
                _serialPort.DataBits = 8;

                // ...
            }
            catch (Exception ex)
            {
                // ...
            }
        }

        private async Task StartDevices()
        {
            await StartSensors();
            await StartSweeping();
        }

        private async Task StartSweeping()
        {
            var sonarCoordinator = await InitSonarCoordinator();
            sonarCoordinator.Start();
        }

        private async Task<SonarCoordinator> InitSonarCoordinator()
        {
            var pca9685ServoContoller = await InitI2cServoController();
            var sonarCoordinator = new SonarCoordinator(pca9685ServoContoller, _devices.Single(d => d.Key == DeviceName("I2C1", ArduinoSlaveAddress, null)).Value as ArduinoSensor);

            sonarCoordinator.PositionFound += SonarCoordinator_PositionFound;
            sonarCoordinator.SensorException += SonarCoordinator_SensorException;
            _devices.Add("sonarCoordinator", sonarCoordinator);
            return sonarCoordinator;
        }

        private void SonarCoordinator_SensorException(object sender, ArduinoBridge.ExceptionEventArgs e)
        {
            var message = $"Error Received from Sensor : \"{e.Message} \"";
            _logIErrorAction(message, e.Exception);

            lock (_lock)
            {
                MessageObject.Error = e.Exception;
                PostMessageToAPI();
            }
        }

        private void SonarCoordinator_PositionFound(object sender, PositionalDistanceEventArgs e)
        {
            var message = $"Sonar Result Received {e.RawValue} Distance {e.Proximity} mm at Position {e.Angle}";
            _logger.LogInfo(message);

            lock (_lock)
            {
                MessageObject.Error = null;
                MessageObject.SonarAngle = e.Angle;
                MessageObject.SonarSensorDistance = e.Proximity;
                MessageObject.SonarSensorRaw = e.RawValue;
                PostMessageToAPI();
            }
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _logger.LogInfo(reason.ToString());
            _readCancellationTokenSource.Cancel();
            _runTask = false;
        }

        private void SetupLogging()
        {
            _logInfoAction = (message) => Debug.WriteLine(message);

            _logIErrorAction = (message, exception) =>
            {
                message = $"{message} - {exception.Message}";
                Debug.WriteLine(message);
            };

            _logger = new Logger(_logInfoAction, _logIErrorAction);
        }

        private Task StartSensors()
        {
            IEnumerable<Task> devicesToStart = new[]
            {
                //InitSimulator(),
                //InitI2cVCNL4000(),
                InitI2cADS1115(0x4A, 1),
                InitI2cADS1115(ADS1115_Constants.ADS1115_ADDRESS.GetHashCode(), 1),
                InitArduinoI2C()
            };

            return Task.WhenAll(devicesToStart)
                .ContinueWith(all => _devices.ForEach(d =>
                    d.Value.Start()
                ));
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private async Task<PCA9685ServoContoller> InitI2cServoController()
        {
            var device = await FindI2CDevice(_I2CBus, ServoControllerAddress, I2cBusSpeed.FastMode, I2cSharingMode.Shared);

            var servoController = new PCA9685ServoContoller(device);
            return servoController;
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
            var device = await FindI2CDevice(_I2CBus, VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode(),I2cBusSpeed.FastMode, I2cSharingMode.Shared);

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

            _devices.Add(DeviceName(BusName, (byte)VCNL4000_Constants.VCNL4000_ADDRESS.GetHashCode(), null), vcnl4000);
        }
        // ReSharper disable once InconsistentNaming
        private async Task InitI2cADS1115(int slaveAddress, byte channel)
        {
            var device = await FindI2CDevice(_I2CBus, slaveAddress, I2cBusSpeed.FastMode, I2cSharingMode.Shared);

            IADS1115Device ads1115 = new ADS1115Device(device, channel);
            ads1115.ChannelChanged += Ads1115ChannelChanged;
            _devices.Add(DeviceName(BusName, (byte)slaveAddress, null), ads1115);
        }
        private async Task InitArduinoI2C()
        {
            var device = await FindI2CDevice(_I2CBus, ArduinoSlaveAddress, I2cBusSpeed.FastMode, I2cSharingMode.Shared);

            var arduino = new ArduinoSensor(device, 25);
            _devices.Add(DeviceName(BusName, ArduinoSlaveAddress, null), arduino);
        }
        private async Task<DeviceInformation> FindI2CController(string busName)
        {
            var aqs = I2cDevice.GetDeviceSelector(busName);              /* Get a selector string that will return all I2C controllers on the system */
            var dis = await DeviceInformation.FindAllAsync(aqs);         /* Find the I2C bus controller device with our selector string           */
            if (dis.Count != 0) return dis[0];

            _logger.LogException("No I2C controllers were found on the system", new Exception("Unexpected Error"));
            return null;
        }
        private async Task<I2cDevice> FindI2CDevice(DeviceInformation bus, int slaveAddress, I2cBusSpeed busSpeed, I2cSharingMode sharingMode)
        {
            var settings = new I2cConnectionSettings(slaveAddress)
            {
                BusSpeed = busSpeed,
                SharingMode = sharingMode,
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

            lock (_lock)
            {
                MessageObject.Error = null;
                MessageObject.ProximitySensorRaw = e.RawValue;
                MessageObject.ProximitySensorDistance = e.Proximity;
                PostMessageToAPI();
            }

        }
        private void Vcnl4000_AmbientLightReceived(object sender, AmbientLightEventArgs e)
        {
            var message = $"Ambient Light Received: {e.RawValue}";
            _logInfoAction(message);

            lock (_lock)
            {
                MessageObject.Error = null;
                MessageObject.AmbientLight = e.RawValue;
                PostMessageToAPI();
            }

        }
        private void SensorException_Handler(object sender, IExceptionEventArgs e)
        {
            var message = $"Error Received from Sensor : \"{e.Message} \"";
            _logIErrorAction(message, e.Exception);

            lock (_lock)
            {
                MessageObject.Error = e.Exception;
                PostMessageToAPI();
            }
        }
        private void Ads1115ChannelChanged(object sender, ChannelReadingDone e)
        {

            lock (_lock)
            {
                var convertedDistance = _sharpSensorConverter.Convert(e.RawValue);
                var message = $"Device {e.SlaveAddress} Channel {e.Channel + 1} Message Received - Raw Value {e.RawValue} - Converted Distance {convertedDistance:F2} mm";
                _logInfoAction(message);

                MessageObject.Error = null;

                var reading = MessageObject.IRSensorReadings
                    .DefaultIfEmpty(new IRSensorReading
                    {
                        IRSensorDistance = convertedDistance,
                        IRSensorRaw = e.RawValue,
                        SlaveAddress = e.SlaveAddress
                    }).First(s => s.SlaveAddress == e.SlaveAddress);

                reading.SlaveAddress = e.SlaveAddress;
                reading.IRSensorDistance = convertedDistance;
                reading.IRSensorRaw = e.RawValue;

                if (!MessageObject.IRSensorReadings.Any(s => s.SlaveAddress == e.SlaveAddress))
                {
                    MessageObject.IRSensorReadings.Add(reading);
                }
                
                PostMessageToAPI();
            }

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


        /// <summary>
        /// ListAvailablePorts
        /// - Use SerialDevice.GetDeviceSelector to enumerate all serial devices
        /// - Attaches the DeviceInformation to the ListBox source so that DeviceIds are displayed
        /// </summary>
        private async Task ListAvailablePorts()
        {
            try
            {
                var aqs = SerialDevice.GetDeviceSelector();
                var devices = await DeviceInformation.FindAllAsync(aqs);

                foreach (var device in devices)
                {
                    _listOfDevices.Add(device);
                }
            }
            catch (Exception ex)
            {
                await _logger.LogException("An error occured listing Serial Devices", ex);
            }
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        private async Task Listen()
        {
            try
            {
                if (_serialPort == null) return;

                _dataReaderObject = new DataReader(_serialPort.InputStream);

                // keep reading the serial input
                while (true)
                {
                    var msg = await ReadAsync(_readCancellationTokenSource.Token);
                    _logger?.LogInfo(msg);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType().Name == "TaskCanceledException")
                {
                    await _logger?.LogInfo("Reading task was cancelled, closing device and cleaning up");
                    CloseDevice();
                }
                else
                {
                    await _logger?.LogException("An error occured listening to the serial port", ex);
                }
            }
            finally
            {
                // Cleanup once complete
                if (_dataReaderObject != null)
                {
                    _dataReaderObject.DetachStream();
                    _dataReaderObject = null;
                }
            }
        }
        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            const uint readBufferLength = 1024;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            _dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

            // Create a task object to wait for data on the serialPort.InputStream
            //try
            //{
            //    var buffer = new byte[readBufferLength];
            //    _dataReaderObject.ReadBytes(buffer);
            //}
            //catch (Exception ex)
            //{

            //}
            
            var loadAsyncTask = _dataReaderObject.LoadAsync(readBufferLength).AsTask(cancellationToken);

            // Launch the task and wait
            var bytesRead = await loadAsyncTask;
            return bytesRead > 0 ? _dataReaderObject.ReadString(bytesRead) : null;
        }

        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (_readCancellationTokenSource == null) return;

            if (!_readCancellationTokenSource.IsCancellationRequested)
            {
                _readCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// CloseDevice:
        /// - Disposes SerialDevice object
        /// - Clears the enumerated device Id list
        /// </summary>
        private void CloseDevice()
        {
            CancelReadTask();
            _serialPort?.Dispose();
            _serialPort = null;
            _listOfDevices.Clear();
        }
    }
}
