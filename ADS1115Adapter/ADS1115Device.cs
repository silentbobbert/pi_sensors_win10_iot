﻿using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;
using Windows.System.Threading;
using static ADS1115Adapter.ADS1115_Constants;

namespace ADS1115Adapter
{
    public class ADS1115Device : IADS1115Device
    {
        private I2cDevice ads1115;
        private ThreadPoolTimer _ads1115Timer;
        public ADS1115Device(I2cDevice device)
        {
            ads1115 = device;
        }

        public void Dispose()
        {
            ads1115.Dispose();
            _ads1115Timer = null;
        }

        public void Start()
        {
            _ads1115Timer = ThreadPoolTimer.CreatePeriodicTimer(ads1115_tick, TimeSpan.FromMilliseconds(500));
        }

        private void ads1115_tick(ThreadPoolTimer timer)
        {
            var reading = readADC_SingleEnded(0);
            System.Diagnostics.Debug.WriteLine($"Reading from ADC Converter: {reading}");
        }

        private int readADC_SingleEnded(byte channel)
        {
            if (channel > 3)
            {
                return 0;
            }

            var config = Set_Defaults();

            // Set single-ended input channel
            switch (channel)
            {
                case (0):
                    config |= (byte) ADS1115_REG_CONFIG_MUX_SINGLE_0.GetHashCode();
                    break;
                case (1):
                    config |= (byte) ADS1115_REG_CONFIG_MUX_SINGLE_1.GetHashCode();
                    break;
                case (2):
                    config |= (byte) ADS1115_REG_CONFIG_MUX_SINGLE_2.GetHashCode();
                    break;
                case (3):
                    config |= (byte) ADS1115_REG_CONFIG_MUX_SINGLE_3.GetHashCode();
                    break;
            }

            // Set 'start single-conversion' bit
            config |= (byte) ADS1115_REG_CONFIG_OS_SINGLE.GetHashCode();

            return GetReadingFromConverter(config);
        }

        private static byte Set_Defaults()
        {
            // Start with default values
            var config = (byte) (ADS1115_REG_CONFIG_CQUE_NONE.GetHashCode() |    // Disable the comparator (default val)
                                ADS1115_REG_CONFIG_CLAT_NONLAT.GetHashCode() |   // Non-latching (default val)
                                ADS1115_REG_CONFIG_CPOL_ACTVLOW.GetHashCode() |  // Alert/Rdy active low   (default val)
                                ADS1115_REG_CONFIG_CMODE_TRAD.GetHashCode() |    // Traditional comparator (default val)
                                ADS1115_REG_CONFIG_DR_1600SPS.GetHashCode() |    // 1600 samples per second (default)
                                ADS1115_REG_CONFIG_MODE_SINGLE.GetHashCode());   // Single-shot mode (default)

            // Set PGA/voltage range
            //config |= (ushort)GetConstantAsByte("ADS1115_REG_CONFIG_PGA_6_144V"); // +/- 6.144V range (limited to VDD +0.3V max!)
            //config |=(byte)ADS1115_REG_CONFIG_PGA_1_024V.GetHashCode();
            config |= (byte)ADS1115_REG_CONFIG_PGA_6_144V.GetHashCode();
            return config;
        }

        private int GetReadingFromConverter(byte config)
        {
            // Write config register to the ADC
            byte[] pointerCommand = {(byte) ADS1015_REG_POINTER_CONFIG.GetHashCode(), config};
            ads1115.Write(pointerCommand);

            var dataBuffer = new byte[2];

            Task.Delay(TimeSpan.FromMilliseconds(ADS1115_CONVERSIONDELAY.GetHashCode()))
                .ContinueWith(async t =>
                {
                    await t;
                    pointerCommand = new[] {(byte) ADS1015_REG_POINTER_CONFIG.GetHashCode()};

                    ads1115.WriteRead(pointerCommand, dataBuffer);
                }).Wait();


            // Read the conversion results
            // Shift 12-bit results right 4 bits for the ADS1015
            //return (UInt16)(GetValue16("ADS1115_ADDRESS", "ADS1115_REG_POINTER_CONVERT") >> BitShift);
            var rawReading = dataBuffer[0] << 8 | dataBuffer[1];
            return rawReading;
        }
    }
}