using System;
using System.Linq.Expressions;
using System.Threading;
using Windows.Devices.I2c;
using Iot.Common;
using Iot.Common.Utils;

namespace VCNL4000Adapter
{
    public class VCNL4000Device : ICommonI2CDevice
    {
        private readonly ILogger _logger;
        private readonly I2cDevice _device;

        public VCNL4000Device(ILogger logger, I2cDevice device, byte irCurrent_mA = 20)
        {
            _logger = logger;
            _device = device;
            SetCurrentOnIRLED(irCurrent_mA);
        }

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
                //sensor.WriteReadPartial(tempCommand, tempData);
                //var rawTempReading = tempData[0] << 8 | tempData[1];
                //var tempRatio = rawTempReading / (float)65536;
                //double temperature = (-46.85 + (175.72 * tempRatio)) * 9 / 5 + 32;
                //System.Diagnostics.Debug.WriteLine("Temp: " + temperature.ToString());
            }
        }

        public decimal Proximity
        {
            get
            {
                var commandRegister = ReadCommandRegister();
                byte[] proximityCommand = { (byte) (commandRegister | (byte) VCNL4000_Constants.VCNL4000_MEASUREPROXIMITY.GetHashCode())};

                //byte[] proximityCommand = { commandRegister , (byte)VCNL4000_Constants.VCNL4000_MEASUREPROXIMITY.GetHashCode() };

                _device.Write(proximityCommand);

                SpinWait.SpinUntil(() => ProximityReady, TimeSpan.FromSeconds(10));
                if (ProximityReady)
                {
                    var readProximityCommand = new[] {(byte) VCNL4000_Constants.VCNL4000_PROXIMITYDATA_1.GetHashCode()};
                    var proximityBuffer = new byte[2];

                    _device.WriteRead(readProximityCommand, proximityBuffer);
                    var rawProximityReading = proximityBuffer[0] << 8 | proximityBuffer[1];
                    return rawProximityReading;
                }
                return 0;

            }
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
    }
}
