using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservatorySafety.Core.Model
{
  public class EquipmentDomeInfo
  {
    public string? ShutterStatus { get; set; }   // ShutterOpen, ShutterClosed, etc.
    public bool Slewing { get; set; }
    public bool AtPark { get; set; }
    public bool Connected { get; set; }
  }

}
