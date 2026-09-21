using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zhaoxi.IntelligentWaterStorageMonitorSystem.Models
{
    public class TrendData
    {
        public DateTime CurTime { get; set; }
        public float MainOutPressure {  get; set; }
        public float MainOutFlow {  get; set; }
    }
}
