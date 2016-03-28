using System;

namespace VCNL4000Adapter
{
    public struct VCNL4000Settings
    {
        public byte IrCurrent_mA { get; set; }
        public int SensorTimeOut { get; set; }
        public int SensorPollInterval { get; set; }
        public double FirstPolynomial { get; set; }
        public double SecondPolynomial { get; set; }
        public double Dy { get; set; }
    }
}