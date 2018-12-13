using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlameSensor.Models
{
    public class Reading
    {
        public DateTime ReadingDateTime { get; set; }

        public double FlameLevel { get; set; }

        public int FlameOn { get; set; }
    }
}
