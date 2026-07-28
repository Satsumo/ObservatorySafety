
using System.Runtime.Serialization;

namespace ObservatorySafety.Core.Status
{
  public enum MonitorType
  {
    [EnumMember(Value = "Power.Status")]
    PowerStatus,

    [EnumMember(Value = "DarkDragon.Mount.Sensor")]
    DarkDragonMountSensor,

    [EnumMember(Value = "Dragonfly")]
    Dragonfly,

    [EnumMember(Value = "NINA")]
    NINA
  }
}
