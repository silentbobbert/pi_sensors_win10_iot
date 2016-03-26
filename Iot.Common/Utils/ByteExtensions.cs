using System;

namespace Iot.Common.Utils
{
    public static class ByteExtensions
    {
        public static char[] ConvertByteToBitArray(this byte data)
        {
            var s = Convert.ToString(data, 2);
            var bits = s.PadLeft(8, '0').ToCharArray();
            return bits;
        }
    }
}
