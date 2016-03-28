using System;

namespace VCNL4000Adapter
{
    public class ProximityConverter
    {
        private readonly double _dy;
        private readonly double _firstPolynomial;
        private readonly double _secondPolynomial;

        public ProximityConverter(double firstPolynomial, double secondPolynomial, double dy)
        {
            _firstPolynomial = firstPolynomial;
            _secondPolynomial = secondPolynomial;
            _dy = dy;
        }

        public double GetDistance(double data)
        {
            var prox = Math.Sqrt(1 / ((_firstPolynomial * data * data) + _secondPolynomial * data - _dy));
            return prox;
        }
    }
}
