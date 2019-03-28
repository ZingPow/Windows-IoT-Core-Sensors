using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMSx003_ParticleSensor.Models
{
    public class CountsLow
    {
        public DateTime ReadingDateTime { get; set; }

        public uint PM2_5Count { get; set; }
        public uint PM5_0Count { get; set; }
        public uint PM10_0Count { get; set; }
    }
}

