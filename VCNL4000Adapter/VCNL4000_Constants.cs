namespace VCNL4000Adapter
{
    // ReSharper disable InconsistentNaming
    public enum VCNL4000_Constants
    {
        VCNL4000_ADDRESS = 0x13,
        VCNL4000_COMMAND = 0x80,
        VCNL4000_PRODUCTID = 0x81,
        VCNL4000_IRLED = 0x83,
        VCNL4000_AMBIENTPARAMETER = 0x84,
        VCNL4000_AMBIENTDATA = 0x85,
        VCNL4000_PROXIMITYDATA_1 = 0x87,
        VCNL4000_PROXIMITYDATA_2 = 0x88,
        VCNL4000_SIGNALFREQ = 0x89,
        VCNL4000_PROXINITYADJUST = 0x8A,
        VCNL4000_3M125 = 0,
        VCNL4000_1M5625 = 1,
        VCNL4000_781K25 = 2,
        VCNL4000_390K625 = 3,
        VCNL4000_MEASUREAMBIENT = 0x10,
        VCNL4000_MEASUREPROXIMITY = 0x08,
        VCNL4000_AMBIENTREADY = 0x40,
        VCNL4000_PROXIMITYREADY = 0x20,
        dx = 5250,
        dy = 2370
    }
    // ReSharper enable InconsistentNaming
}