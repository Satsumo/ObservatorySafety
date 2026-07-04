using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservatorySafety.Core.Model
{
  public class EquipmentMountInfo
  {
    public bool AtPark { get; set; }
    public bool Slewing { get; set; }
    public bool TrackingEnabled { get; set; }
    public bool Connected { get; set; }
  }

}
