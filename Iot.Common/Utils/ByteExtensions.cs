using System;
using System.Collections;
using System.Linq;

namespace Iot.Common.Utils
{
    public static class ByteExtensions
    {
        private static bool FlagIsTrue(bool[] flags, int bitIndex)
        {
            var bitIsTrue = flags[bitIndex];
            return bitIsTrue;
        }
        public static bool FlagIsTrue(this byte flagByte, int bitIndex, bool reverse)
        {
            return FlagIsTrue(new[] {flagByte}, bitIndex, reverse);
        }
        public static bool FlagIsTrue(this byte[] flagBytes, int bitIndex, bool reverse)
        {
            var flags = new BitArray(flagBytes).Cast<bool>();
            if(reverse)
                flags = flags.Reverse();
            return FlagIsTrue(flags.ToArray(), bitIndex);
        }
    }
}
