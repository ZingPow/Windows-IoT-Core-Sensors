using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMSx003_ParticleSensor.Models
{
    public class CountsHigh
    {
        public DateTime ReadingDateTime { get; set; }

        public uint PM0_3Count { get; set; }
        public uint PM0_5Count { get; set; }
        public uint PM1_0Count { get; set; }
    }
}
