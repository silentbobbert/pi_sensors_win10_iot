namespace VCNL4000Adapter
{
    public class ProximityConverter
    {
        private readonly decimal _dx;
        private readonly decimal _dy;

        public ProximityConverter(decimal dx, decimal dy)
        {
            _dx = dx;
            _dy = dy;
        }

        public decimal GetDistance(decimal data)
        {
            // ReSharper disable once InconsistentNaming
            var distanceIn_mm = (_dx / data) - _dy;
            return distanceIn_mm;
        }
    }
}
