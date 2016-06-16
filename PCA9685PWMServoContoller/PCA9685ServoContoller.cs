using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Iot.Common;

namespace PCA9685PWMServoContoller
{

    /// <summary>
    /// A class to provide interaction with the NXP PCA9685 integrated circuit or expansion modules that use it 
    /// such as the Adafruit 16-Channel 12-bit PWM/Servo Driver PCA9685 module.
    /// https://www.adafruit.com/product/815
    /// 
    /// The PCA9685 is an I2C-bus controlled 16-channel PWM/LED controller.  Each output has its
    /// own 12-bit resolution (4096 steps) fixed frequency individual PWM controller that operates
    /// at a programmable frequency from a typical of 24 Hz to 1526 Hz with a duty cycle that is
    /// adjustable from 0 % to 100 %.  All outputs are set to the same PWM frequency.
    /// </summary>
    /// <remarks>
    /// Version 2015.12.27.0
    /// Developed by: Chris Leavitt.
    /// </remarks>
    public sealed class PCA9685ServoContoller 
    {
        /// <summary>
        /// The I2C device instance to be controlled. 
        /// </summary>
        private I2cDevice _device;

        /// <summary>
        /// The number of channels supported by the expansion module.
        /// </summary>
        private const int _ChannelCount = 16;

        public PCA9685ServoContoller(I2cDevice device)
        {
            _device = device;
            Task.Delay(100).Wait();
            // Set default configuration to ensure a known state.
            //ResetDevice();
        }

        /// <summary>
        /// Gets the number of channels supported by the device.
        /// </summary>
        public int ChannelCount => _ChannelCount;

        /// <summary>
        /// Gets the 7-bit I2C bus address of the device.
        /// </summary>
        public int I2CAddress => _device.ConnectionSettings.SlaveAddress;

        /// <summary>
        /// Gets the plug and play device identifier of the inter-integrated circuit (I2C) bus controller for the device.
        /// </summary>
        public string I2CDeviceId => _device.DeviceId;

        /// <summary>
        /// Gets the bus speed used for connecting to the inter-integrated circuit
        //  (I2C) device. The bus speed is the frequency at which to clock the I2C bus when
        //  accessing the device.
        /// </summary>
        public I2cBusSpeed I2CBusSpeed => _device.ConnectionSettings.BusSpeed;

        /// <summary>
        /// Gets the current PWM update rate.  The frequency in Hz with a valid range of 24 Hz to 1526 Hz.
        /// </summary>
        public int PWMUpdateRate => 25000000 / ((ReadRegister(Register.PRESCALE) + 1) * 4096);

        /// <summary>
        /// Gets the current device Mode register 1 values.
        /// This register stores device level configuration details.
        /// </summary>
        /// <returns>A string of binary characters representing the single byte of data in the register.</returns>
        /// <remarks>
        /// For diagnostic use.  See section 7.3.1 of manufacturer documentation or command constants in this class 
        /// for details on what each bit in the byte does.
        /// </remarks>
        public string Mode1Config => ByteToBinaryString(ReadRegister(Register.MODE1));

        /// <summary>
        /// Gets the current device Mode register 2 values.
        /// This register stores device level configuration details.
        /// </summary>
        /// <returns>A string of binary characters representing the single byte of data in the register.</returns>
        /// <remarks>
        /// For diagnostic use.  See section 7.3.1 of manufacturer documentation or command constants in this class 
        /// for details on what each bit in the byte does.
        /// </remarks>
        public string Mode2Config => ByteToBinaryString(ReadRegister(Register.MODE2));

        public static int ChannelCount1 => _ChannelCount;



        public void Dispose()
        {
            _device.Dispose();
        }


        #region " Methods (Public / Device Configuration) "

        /// <summary>
        /// Sets the PWM update rate.
        /// </summary>
        /// <param name="frequency">The frequency in Hz with a valid range of 24 Hz to 1526 Hz.</param>
        /// <remarks>
        /// Actual frequency set may vary slightly from the specified frequency parameter value due to rounding to 8 bit precision.
        /// Data sheet: 7.3.5 PWM frequency PRE_SCALE.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified frequency is out of the range supported by the integrated circuit.</exception>
        public void SetPwmUpdateRate(int frequency)
        {

            // The maximum PWM frequency is ~1526 Hz if the PRE_SCALE register is set to "0x03".
            // The minimum PWM frequency is ~24 Hz if the PRE_SCALE register is set to "0xFF".
            if (frequency < 24 || frequency > 1526)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Value must be in the range of 24 to 1526.");
            }

            // Calculate the "prescale register" value required to accomplish the specified target frequency.
            decimal preScale = 25000000; // 25 MHz.
            preScale /= 4096; // 12 bit.
            preScale /= frequency;
            preScale -= 1;
            preScale = Math.Round(preScale, MidpointRounding.AwayFromZero);

            // Debug output.
            //System.Diagnostics.Debug.WriteLine("Setting PWM frequency to {0} Hz using prescale value {1}", frequency, PreScale);

            // The PRE_SCALE register can only be set when the device is in sleep mode (oscillator is disabled).
            Sleep();

            // Set the prescale value to change the frequency.
            WriteRegister(Register.PRESCALE, (byte)Math.Floor(preScale));

            // Return to the normal operating mode.
            Wake();
            Restart();

        }

        /// <summary>
        /// Puts the device in sleep (low power) mode.  Oscillator off.
        /// The channel outputs cannot be controlled while in sleep mode.
        /// </summary>
        public void Sleep()
        {
            // Get the current bit values in the device's MODE 1 control/config register so those bit values can be persisted while changing the target bit.
            var registerValue = ReadRegister(Register.MODE1);

            WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SLEEP_MASK) | (byte)Command.SLEEP)); // Go to sleep.

            // The SLEEP bit must be logic 0 for at least 5 ms, before a logic 1 is written into the RESTART bit.
            Task.Delay(10).Wait();
        }

        /// <summary>
        /// Wakes the device from sleep (low power) mode.  Oscillator on.
        /// </summary>
        public void Wake()
        {
            // Get the current bit values in the device's MODE 1 control/config register so those bit values can be persisted while changing the target bit.
            var registerValue = ReadRegister(Register.MODE1);

            WriteRegister(Register.MODE1, ((registerValue & (byte)Command.WAKE_MASK) | (byte)Command.WAKE)); // Wake from sleep.

            // The SLEEP bit must be logic 0 for at least 5 ms, before a logic 1 is written into the RESTART bit.
            Task.Delay(10).Wait();
        }

        /// <summary>
        /// Resumes the most recent (if any) channel output after awaking the device from sleep mode.
        /// Call this method after calling Wake() to restart any output that was active prior to sleep.
        /// </summary>
        /// <remarks>
        /// If the PCA9685 is operating and the chip is put into to sleep mode without stopping any of the 
        /// PWM channels, the RESTART bit will be set to 1. The contents of each PWM register (i.e. channel config) are saved 
        /// while in sleep mode and can be restarted by calling this method once the chip is awake.
        /// NOTE: This is used to resume output after awaking from sleep mode.  This is NOT used to perform a "power on reset" or software reset of the device. 
        /// Data sheet: 7.3.1.1 "Restart mode".
        /// </remarks>
        public void Restart()
        {
            // Get the current bit values in the device's MODE 1 control/config register so those bit values can be persisted while changing the target bit.
            var registerValue = ReadRegister(Register.MODE1);

            WriteRegister(Register.MODE1, (registerValue | (byte)Command.RESTART)); // Restart.
        }

        

        /// <summary>
        /// Resets the device/module configuration to default settings and sets all output channels to off.
        /// Can be used to return the device to a known state.
        /// </summary>
        public void ResetDevice()
        {
            // Set default configuration to ensure a known state.
            SetFullOff(PwmChannel.All);
            WriteRegister(Register.MODE1, (byte)Command.DEFAULT_CONFIG);
        }
        //public void ResetDevice()
        //{
        //    SetAllCall(false);

        //    // Set default configuration to ensure a known state.
        //    SetFullOff(PwmChannel.All);
        //    WriteRegister(Register.MODE1, (byte)Command.DEFAULT_CONFIG);
        //    WriteRegister(Register.MODE2, (byte)Command.OUTDRV);

        //    Task.Delay(5).Wait(); //wait for oscillator
        //    var mode1 = ReadRegister(Register.MODE1);
        //    mode1 = (byte)(mode1 & (byte) Command.SLEEP); // wake up (reset sleep)
        //    WriteRegister(Register.MODE1, mode1);
        //    Task.Delay(5).Wait(); //wait for oscillator
        //}

    #region " Methods (Public / Advanced Device Configuration) "

    // UNDONE: This device's "all call" and "sub address" functionality have not been fully implemented and are disabled by default.
    // It is possible to enable/disable these features in the devices configuration using methods provided by this class, but this class does not make use of the features beyond that.

    /// <summary>
    /// Enables or disables the I2C "all call" address for the device.
    /// Allow multiple modules to be controlled with a single address.  
    /// All modules will respond to the all call address if enabled.
    /// </summary>
    /// <param name="enable">True to enable, false to disable.</param>
    public void SetAllCall(bool enable)
        {
            var registerValue = ReadRegister(Register.MODE1);

            if (enable)
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.ALLCALL_MASK) | (byte)Command.ALLCALL_ENABLE)); // Enable address.
            else
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.ALLCALL_MASK) | (byte)Command.ALLCALL_DISABLE)); // Disable address.
        }

        /// <summary>
        /// Enables or disables the I2C "sub address" for the device.
        /// Allow multiple channels on a device to be controlled with a single address.  
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        public void SetSubAddr1(bool enable)
        {
            var registerValue = ReadRegister(Register.MODE1);

            if (enable)
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR1_MASK) | (byte)Command.SUBADR1_ENABLE)); // Enable address.
            else
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR1_MASK) | (byte)Command.SUBADR1_DISABLE)); // Disable address.
        }

        /// <summary>
        /// Enables or disables the I2C "sub address" for the device.
        /// Allow multiple channels on a device to be controlled with a single address.  
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        public void SetSubAddr2(bool enable)
        {
            var registerValue = ReadRegister(Register.MODE1);

            if (enable)
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR2_MASK) | (byte)Command.SUBADR2_ENABLE)); // Enable address.
            else
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR2_MASK) | (byte)Command.SUBADR2_DISABLE)); // Disable address.
        }

        /// <summary>
        /// Enables or disables the I2C "sub address" for the device.
        /// Allow multiple channels on a device to be controlled with a single address.  
        /// </summary>
        /// <param name="enable">True to enable, false to disable.</param>
        public void SetSubAddr3(bool enable)
        {
            var registerValue = ReadRegister(Register.MODE1);

            if (enable)
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR3_MASK) | (byte)Command.SUBADR3_ENABLE)); // Enable address.
            else
                WriteRegister(Register.MODE1, ((registerValue & (byte)Command.SUBADR3_MASK) | (byte)Command.SUBADR3_DISABLE)); // Disable address.
        }

        #endregion

        #endregion

        #region " Methods (Public / Device Channel Control) "

        /// <summary>
        /// Sets a channels PWM on/off range.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        /// <param name="on">The on cycle value on a scale of 0 to 4095.</param>
        /// <param name="off">The off cycle value on a scale of 0 to 4095.</param>
        /// <remarks>
        /// Data sheet: 7.3.3 "LED output and PWM control".
        /// Data sheet: 7.3.4 "ALL_LED_ON and ALL_LED_OFF control".
        /// </remarks>
        public void SetPwm(PwmChannel channel, int on, int off)
        {
            // The PWM outputs have a 12 bit precision for pulse width modulation, so two register writes (one byte each) are needed to set the total value.
            // For example: The max PWM value (4095) would be written to the device registers as byte values 00001111 and 11111111.
            //              The min PWM value (zero) would be written to the device registers as byte values 00000000 and 00000000.
            //              The mid PWM value (2048) would be written to the device registers as byte values 00001000 and 00000000.
            // This process (two register writes) must be used to set both the on and off cycle values for a total of 4 register/byte writes.
            // The 13th bit in both the On and Off registers has special meaning.  Setting this bit to 1 (decimal 4096 or above) will turn the channel fully on or off 
            //   depending on which register it is applied to.  In this case the remaining 12 less significant bits will be ignored and the channel will remain constant 
            //   on or off rather than emitting a pulse.  All other bits (14 through 16) are non-writable and reserved by the device.
            // For example: SetPwm(channel, 4096, 0) would set the "On" registers to 00010000 00000000 which would disable PWM for the channel and set it to fully/constant on.
            //              SetPwm(channel, 0, 4096) would have the same affect on the "Off" registers causing the channel to be fully/constant off.

            // Set the on cycle for the specified channel register.
            WriteRegister(Register.LED0_ON_L + 4 * (int)channel, on & 0xFF); // Set the 8 least significant bits.  0000XXXXXXXX.
            WriteRegister(Register.LED0_ON_H + 4 * (int)channel, on >> 8); // Set the remaining 4 most significant bits.  XXXX00000000.

            // Set the off cycle for the specified channel register.
            WriteRegister(Register.LED0_OFF_L + 4 * (int)channel, off & 0xFF); // Set the 8 least significant bits.  0000XXXXXXXX.
            WriteRegister(Register.LED0_OFF_H + 4 * (int)channel, off >> 8); // Set the remaining 4 most significant bits.  XXXX00000000.
        }

        /// <summary>
        /// Get a channels PWM on cycle value.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        /// <returns>The On cycle value on a scale of 0 to 4095, or 4096 for constant on.</returns>
        public int GetPwmOn(PwmChannel channel) => (ReadRegister(Register.LED0_ON_H + 4 * (int)channel) << 8) + ReadRegister(Register.LED0_ON_L + 4 * (int)channel);

        /// <summary>
        /// Get a channels PWM off cycle value.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        /// <returns>The Off cycle value on a scale of 0 to 4095, or 4096 for constant off.</returns>
        public int GetPwmOff(PwmChannel channel) => (ReadRegister(Register.LED0_OFF_H + 4 * (int)channel) << 8) + ReadRegister(Register.LED0_OFF_L + 4 * (int)channel);

        /// <summary>
        /// Set a channel to fully on or off.  Constant output without pulse width modulation.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        /// <param name="fullOn">If set to <c>true</c>, channel is set fully on; otherwise fully off.</param>
        public void SetFull(PwmChannel channel, bool fullOn)
        {
            if (fullOn)
                SetFullOn(channel);
            else
                SetFullOff(channel);
        }

        /// <summary>
        /// Set a channel to fully on.  Constant output without pulse width modulation.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        public void SetFullOn(PwmChannel channel)
        {
            // Set channel On registers to 0001 0000 0000 0000 and Off registers to 0.
            // Setting the 13th bit to 1 sets the channel to constant output (either on or off).
            SetPwm(channel, 4096, 0);
        }

        /// <summary>
        /// Set a channel to fully off.  Constant output without pulse width modulation.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        public void SetFullOff(PwmChannel channel)
        {
            // Set channel On registers to 0 and Off registers to 0001 0000 0000 0000.
            // Setting the 13th bit to 1 sets the channel to constant output (either on or off).
            SetPwm(channel, 0, 4096);
        }

        /// <summary>
        /// Sets a channel PWM or LED brightness by duty cycle (percentage) value.
        /// </summary>
        /// <param name="channel">The output channel.</param>
        /// <param name="dutyCycle">The percentage of time that the channel should be on from 0 to 100.</param>
        /// <remarks>
        /// This method provides a simple way of controlling output based on a value of 0 to 100 percent.
        /// Useful when precise control of the PWM start/stop cycles (such as custom offsets or initial delays) is not required.  
        /// </remarks>
        public void SetDutyCycle(PwmChannel channel, double dutyCycle)
        {

            // Zero percent duty cycle.
            if (Math.Abs(dutyCycle) < 0.0001)
            {
                SetFullOff(channel);
                return;
            }

            // 100 percent duty cycle.
            if (Math.Abs(dutyCycle - 100) < 0.0001)
            {
                SetFullOn(channel);
                return;
            }

            // N percent duty cycle.
            if (!(dutyCycle > 0) || !(dutyCycle < 100)) return;

            // Calculate and set the number of cycles required to match the specified duty cycle percentage.
            var stopCycle = (int)Math.Round(4095 * dutyCycle / 100, MidpointRounding.AwayFromZero);
            SetPwm(channel, 0, stopCycle);
        }

        #endregion

        #region " Methods (Private / Device Communications) "

        /// <summary>
        /// Writes the specified data to the specified device register.
        /// </summary>
        /// <param name="register">The device register address.</param>
        /// <param name="data">The data to write to the device.</param>
        private void WriteRegister(Register register, int data)
        {
            WriteRegister(register, (byte)data);
        }

        /// <summary>
        /// Writes the specified data to the specified device register.
        /// </summary>
        /// <param name="register">The device register address.</param>
        /// <param name="data">The data to write to the device.</param>
        private void WriteRegister(Register register, byte data)
        {

            // Debug output.
            //System.Diagnostics.Debug.WriteLine("WriteRegister: {0}, data = {1}.", register, ByteToBinaryString(data));
            try
            {
                _device.Write(new[] { (byte)register, data });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            

        }

        /// <summary>
        /// Reads the data from the specified device register.
        /// </summary>
        /// <param name="register">The device register address.</param>
        /// <returns>The data read from the device.</returns>
        private byte ReadRegister(Register register)
        {
            byte result = 0;
            try
            {
                // Initialize the read/write buffers.
                var regAddrBuf = new[] { (byte)register }; // Device register address to write.          
                var readBuf = new byte[1]; // Read buffer to store results read from device.

                // Read from the device.
                // We call WriteRead(), first write the address of the device register to be targeted, then read back the data from the device.
                _device.WriteRead(regAddrBuf, readBuf);
                result = readBuf[0];
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            // Debug output.
            //System.Diagnostics.Debug.WriteLine("ReadRegister: {0}, result = {1}.", register, ByteToBinaryString(result));
            return result;
        }

        #endregion

        #region " Methods (Private / Utility) "

        /// <summary>
        /// Converts and formats a byte of data into a string of binary characters.
        /// For example, 0x04 would be converted to 00000100.
        /// </summary>
        /// <param name="data">The byte data to be converted into a binary string format.</param>
        /// <returns>A string of eight binary characters representing the specified byte of data</returns>
        private string ByteToBinaryString(byte data)
        {
            var value = Convert.ToInt32(data);
            var binaryString = string.Empty;

            while (value > 0)
            {
                binaryString = string.Format("{0}{1}", (value & 1) == 1 ? "1" : "0", binaryString);
                value >>= 1; // Binary shift one position.
            }

            // Format with leading zeros to form a full byte length.
            binaryString = binaryString.PadLeft(8, '0');

            return binaryString;
        }

        

        #endregion

    }

}