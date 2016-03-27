using System;
using System.Threading;
using Windows.Devices.I2c;
using Windows.System.Threading;
using Iot.Common.Utils;

namespace VCNL4000Adapter
{
    public class VCNL4000Device : IVCNL4000Device
    {
        private readonly I2cDevice _device;
        private ThreadPoolTimer _vcnl4000Timer;
        public event EventHandler<ProximtyEventArgs> ProximityReceived;
        public event EventHandler<ExceptionEventArgs> SensorException;
        public event EventHandler<AmbientLightEventArgs> AmbientLightReceived;

        public VCNL4000Device(I2cDevice device, byte irCurrent_mA = 20)
        {
            _device = device;
            SetCurrentOnIRLED(irCurrent_mA);
        }
        /// <summary>
        /// According to the documentation for the VCNL4000 the current value is passed in dec as oppose to hex, and the value you provide is multiplied by 10.
        /// This means if you supply 20 (the max and default) the current is set to 200mA. This is quite high on a low power system, so you may want to try lower values for your application.
        /// </summary>
        /// <param name="tenthOfDesiredCurrent_mA"></param>
        // ReSharper disable once InconsistentNaming
        public void SetCurrentOnIRLED(byte tenthOfDesiredCurrent_mA)
        {
            _device.Write(new[] { (byte)VCNL4000_Constants.VCNL4000_IRLED, tenthOfDesiredCurrent_mA });
        }
        public int ProductId
        {
            get
            {
                byte[] productIdCommand = { (byte) VCNL4000_Constants.VCNL4000_PRODUCTID.GetHashCode() };
                var productData = new byte[1];

                _device.WriteRead(productIdCommand, productData);

                var data = productData[0];
                return data;
            }
        }
        private int RawAmbientLight {
            get
            {
                //RequestSensorToReadAmbientLight();
                SpinWait.SpinUntil(() => AmbientLightReady, TimeSpan.FromMilliseconds(500));

                if (!AmbientLightReady)
                {
                    SensorException?.Invoke(this, new ExceptionEventArgs
                    {
                        Exception = new Exception("Sensor did not respond in an appropriate time period."),
                        Message = "An Error Occured reading the Am Results registers."
                    });
                }

                return !AmbientLightReady ? -1 :
                    RawAmbientLightReading();

            }
        }
        private int RawAmbientLightReading()
        {
            try
            {
                var readAmbientLightCommand = new[] { (byte)VCNL4000_Constants.VCNL4000_AMBIENTDATA.GetHashCode() };
                var ambientBuffer = new byte[2]; //Read VCNL4000_AMBIENTDATA for two bytes long, its a 16 bit value.

                _device.WriteRead(readAmbientLightCommand, ambientBuffer);
                var rawPAmbientReading = ambientBuffer[0] << 8 | ambientBuffer[1];
                return rawPAmbientReading;
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An Error Occured reading the Ambient Light Results registers."
                });
            }
            return -1;
        }
        private void RequestSensorToReadAmbientLight()
        {
            try
            {
                WriteCommandToCommandRegister((byte)VCNL4000_Constants.VCNL4000_MEASUREAMBIENT.GetHashCode());
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An Error Occured writing the request to read the Ambient Light."
                });
            }
        }
        private int RawProximity
        {
            get
            {
                //RequestSensorToReadProximity();
                SpinWait.SpinUntil(() => ProximityReady, TimeSpan.FromMilliseconds(500));

                if (!ProximityReady)
                {
                    SensorException?.Invoke(this, new ExceptionEventArgs
                    {
                        Exception = new Exception("Sensor did not respond in an appropriate time period."),
                        Message = "An Error Occured reading the Proximity Results registers."
                    });
                }

                return !ProximityReady ? -1 : 
                    RawProximityReading();
            }
        }
        private int RawProximityReading()
        {
            try
            {
                var readProximityCommand = new[] { (byte)VCNL4000_Constants.VCNL4000_PROXIMITYDATA_1.GetHashCode() };
                var proximityBuffer = new byte[2]; //Read VCNL4000_PROXIMITYDATA_1 and VCNL4000_PROXIMITYDATA_2 at the same time as they are adjacent register addresses

                _device.WriteRead(readProximityCommand, proximityBuffer);
                var rawProximityReading = proximityBuffer[0] << 8 | proximityBuffer[1];
                return rawProximityReading;
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An Error Occured reading the Proximity Results registers."
                });
            }
            return -1;
        }
        private void RequestSensorToReadProximity()
        {
            try
            {
                WriteCommandToCommandRegister((byte)VCNL4000_Constants.VCNL4000_MEASUREPROXIMITY.GetHashCode());
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An Error Occured writing the request to read the proximity."
                });
            }
        }

        private void RequestSensorToStartCollectingData()
        {
            try
            {
                var command = (byte) (VCNL4000_Constants.VCNL4000_MEASUREPROXIMITY.GetHashCode() | VCNL4000_Constants.VCNL4000_MEASUREAMBIENT.GetHashCode());

                WriteCommandToCommandRegister(command);
            }
            catch (Exception ex)
            {
                SensorException?.Invoke(this, new ExceptionEventArgs
                {
                    Exception = ex,
                    Message = "An Error Occured writing the request to read the proximity."
                });
            }
        }

        private void WriteCommandToCommandRegister(byte commandToWriteToCommandRegister)
        {
            var commandRegister = ReadCommandRegister(); //Get current state of Command register so we dont just blat whats already there.
            byte[] registerCommand = { (byte) (commandRegister | VCNL4000_Constants.VCNL4000_COMMAND.GetHashCode()), commandToWriteToCommandRegister };
            _device.Write(registerCommand);
        }
        private byte ReadCommandRegister()
        {
            var commandRegisterBuffer = new byte[1];
            _device.WriteRead(new[] { (byte)VCNL4000_Constants.VCNL4000_COMMAND }, commandRegisterBuffer);
            return commandRegisterBuffer[0];
        }
        public void Dispose()
        {
            _device.Dispose();
        }
        public void Start()
        {
            _vcnl4000Timer = ThreadPoolTimer.CreatePeriodicTimer(vcnl400_TimerTick, TimeSpan.FromMilliseconds(2000));
        }
        private void vcnl400_TimerTick(ThreadPoolTimer timer)
        {
            RequestSensorToStartCollectingData();
            var proximityArgs = new ProximtyEventArgs { RawValue = RawProximity };
            ProximityReceived?.Invoke(this, proximityArgs);

            var ambientLightEventArgs = new AmbientLightEventArgs { RawValue = RawAmbientLight };
            AmbientLightReceived?.Invoke(this, ambientLightEventArgs);
        }
        private bool ProximityReady
        {
            get
            {
                /*
                 * "10100000" = 0xA0
                 * Bit  7(0) - Config_lock - Ignore
                 * Bit  6(1) - als_data_rdy - Ambient Light sensor ready
                 * Bit  5(2) - prox_data_rdy - Promimity Data ready
                 * Bit  4(3) - als_od - Ambient Light Reading On-demand
                 * Bit  3(4) - prox_od - Proximity Reading On-demand
                 * Bit  2(5) - N/A    
                 * Bit  1(6) - N/A    
                 * Bit  0(7) - N/A               
                 */

                var register = ReadCommandRegister().ConvertByteToBitArray();
                return register[2] == '1';
            }
        }
        private bool AmbientLightReady
        {
            get
            {
                /*
                 * "10100000" = 0xA0
                 * Bit  7(0) - Config_lock - Ignore
                 * Bit  6(1) - als_data_rdy - Ambient Light sensor ready
                 * Bit  5(2) - prox_data_rdy - Promimity Data ready
                 * Bit  4(3) - als_od - Ambient Light Reading On-demand
                 * Bit  3(4) - prox_od - Proximity Reading On-demand
                 * Bit  2(5) - N/A    
                 * Bit  1(6) - N/A    
                 * Bit  0(7) - N/A               
                 */
                var register = ReadCommandRegister().ConvertByteToBitArray();
                return register[1] == '1';
            }
        }
    }
}
