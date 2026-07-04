using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObservatorySafety.Core.Model
{
  public class EquipmentInfoEnvelope
  {
    public EquipmentInfo? Response { get; set; }
    public string? Error { get; set; }
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string? Type { get; set; }
  }

  public class EquipmentInfo
  {
    public EquipmentCameraInfo? Camera { get; set; }
    public EquipmentDomeInfo? Dome { get; set; }
    public EquipmentMountInfo? Mount { get; set; }
    public EquipmentSequenceInfo? Sequence { get; set; }
    public EquipmentSafetyMonitorInfo? SafetyMonitor { get; set; }
  }

}
