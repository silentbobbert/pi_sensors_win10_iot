using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADS1115Adapter
{
    public class ChannelOneReadingDone : EventArgs
    {
        public int RawValue { get; set; }
    }
}
