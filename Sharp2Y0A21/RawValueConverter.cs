using System;
using Iot.Common;

namespace Sharp2Y0A21
{
    public class RawValueConverter : IRawValueConverter
    {
        private readonly double _conversionFactor;
        private readonly double _exponent;

        public RawValueConverter(double conversionFactor, double exponent)
        {
            _conversionFactor = conversionFactor;
            _exponent = exponent;
        }

        public double Convert(int rawValue)
        {
            //4221057.491	1.26814
            var distance = Math.Pow((_conversionFactor/ System.Convert.ToDouble(rawValue)), (1/ _exponent));
            return distance;
        }
    }
}
