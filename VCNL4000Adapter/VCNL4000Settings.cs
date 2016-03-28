using System;

namespace VCNL4000Adapter
{
    public struct VCNL4000Settings
    {
        public byte IrCurrent_mA { get; set; }
        public int SensorTimeOut { get; set; }
        public int SensorPollInterval { get; set; }
        public decimal Dx { get; set; }
        public decimal Dy { get; set; }
    }
}