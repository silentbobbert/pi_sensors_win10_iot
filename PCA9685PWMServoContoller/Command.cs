namespace PCA9685PWMServoContoller
{
    public enum Command
    {
        DEFAULT_CONFIG = 0x00, // 0000 0000.  Disables PCA9685 All Call I2C address (0x70) which is normally active on start-up.

        // Mode register 1, bits 0 to 7.
        // Data sheet: 7.3.1 "Mode register 1, MODE1".
        // Bit 0.
        ALLCALL_ENABLE = 0x01, // 0000 0001.
        ALLCALL_DISABLE = 0x00, // 0000 0000.
        ALLCALL_MASK = 0x7E, // 0111 1110.

        // Bit 1.
        SUBADR3_ENABLE = 0x02, // 0000 0010.
        SUBADR3_DISABLE = 0x00, // 0000 0000.
        SUBADR3_MASK = 0x7D, // 0111 1101.

        // Bit 2.
        SUBADR2_ENABLE = 0x04, // 0000 0100.
        SUBADR2_DISABLE = 0x00, // 0000 0000.
        SUBADR2_MASK = 0x7B, // 0111 1011.

        // Bit 3.
        SUBADR1_ENABLE = 0x08, // 0000 1000.
        SUBADR1_DISABLE = 0x00, // 0000 0000.
        SUBADR1_MASK = 0x77, // 0111 0111.

        // Bit 4.
        WAKE = 0x00, // 0000 0000.
        WAKE_MASK = 0x6F, // 0110 1111.
        SLEEP = 0x10, // 0001 0000.
        SLEEP_MASK = 0x7F, // 0111 1111.

        // Bit 5.  Auto-Increment functionality not implemented.
        // Bit 6.  External clock functionality not implemented.

        // Bit 7.  If the PCA9685 is operating and the user decides to put the chip to sleep without stopping any of the 
        // PWM channels, the RESTART bit (MODE1 bit 7) will be set to 1 at the end of the PWM refresh cycle. The contents 
        // of each PWM register (i.e. channel config) are saved while in sleep mode and can be restarted once the chip is awake.
        // NOTE: This is used to resume output after sleep mode.  This is NOT used to perform a "power on reset" of the device.  
        RESTART = 0x80, // 1000 0000.

        // Mode register 2 commands/functionality not implemented.

        OUTDRV = 0x04,

    }
}
