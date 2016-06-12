namespace PCA9685PWMServoContoller
{
    /// <summary>
    /// The registers available on the control module's integrated circuit.
    /// </summary>
    public enum Register
    {

        /// <summary>
        /// Mode register 1.  Stores global device configuration.
        /// </summary>
        MODE1 = 0x00,

        /// <summary>
        /// Mode register 2.  Stores global device configuration.
        /// </summary>
        MODE2 = 0x01,

        /// <summary>
        /// I2C-bus sub-address 1 register.
        /// </summary>
        SUBADR1 = 0x02,

        /// <summary>
        /// I2C-bus sub-address 2 register.
        /// </summary>
        SUBADR2 = 0x03,

        /// <summary>
        /// I2C-bus sub-address 3 register.
        /// </summary>
        SUBADR3 = 0x04,

        /// <summary>
        /// LED "all call" I2C-bus address register.
        /// </summary>
        ALLCALLADR = 0x05,

        // Base PWM/LED output channel control registers.

        /// <summary>
        /// LED0 On output and brightness control register 1 of 2.  Stores 8 least significant digits.
        /// </summary>
        LED0_ON_L = 0x06,

        /// <summary>
        /// LED0 On output and brightness control register 2 of 2.  Stores 4 most significant digits.
        /// </summary>
        LED0_ON_H = 0x07,

        /// <summary>
        /// LED0 Off output and brightness control register 1 of 2.  Stores 8 least significant digits.
        /// </summary>
        LED0_OFF_L = 0x08,

        /// <summary>
        /// LED0 Off output and brightness control register 2 of 2.  Stores 4 most significant digits.
        /// </summary>
        LED0_OFF_H = 0x09,

        // The remaining output channels use registers 0x0A to 0x45.
        // Rather than defining the remaining channel control registers individually, all other output 
        //   channels will be controlled using multiples of the base channel registers above.
        // Formula = <LED0_Register> + 4 * <ChannelNumber>
        // Example = LED0_OFF_L + 4 * 15

        /// <summary>
        /// Loads all the LEDn_ON_L registers at once.
        /// </summary>
        ALLLED_ON_L = 0xFA,

        /// <summary>
        /// Loads all the LEDn_ON_H registers at once.
        /// </summary>
        ALLLED_ON_H = 0xFB,

        /// <summary>
        /// Loads all the LEDn_OFF_L registers at once.
        /// </summary>
        ALLLED_OFF_L = 0xFC,

        /// <summary>
        /// Loads all the LEDn_OFF_H registers at once.
        /// </summary>
        ALLLED_OFF_H = 0xFD,

        /// <summary>
        /// Prescaler register for PWM output frequency.
        /// </summary>
        PRESCALE = 0xFE,

    }
}
