using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParsPerflog
{
    [Serializable]
    class GaugePoint
    {
        public string Method { get; set; }
        public int Duration { get; set; }
        public DateTime time { get; set; }
    }
}
