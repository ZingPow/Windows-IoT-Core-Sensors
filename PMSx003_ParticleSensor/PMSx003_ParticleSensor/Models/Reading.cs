using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMSx003_ParticleSensor.Models
{
    public class Reading
    {
        public DateTime ReadingDateTime { get; set; }

        public uint PM1_0Concentration { get; set; }

        public uint PM2_5Concentration { get; set; }

        public uint PM10_0Concentration { get; set; }
    }
}
