using System.Runtime.Serialization;

namespace ObservatorySafety.Core.Status
{
  public enum StatusType
  {
    [EnumMember(Value = "Application.Running")]
    ApplicationRunning,

    [EnumMember(Value = "Sequence.Running")]
    SequenceRunning,

    [EnumMember(Value = "Mount.Parked")]
    MountParked,

    [EnumMember(Value = "Roof.Closed")]
    RoofClosed,

    [EnumMember(Value = "Roof.Opened")]
    RoofOpen,

    [EnumMember(Value = "Weather.Safe")]
    WeatherSafe,

    [EnumMember(Value = "Power.On")]
    PowerOn,

    [EnumMember(Value = "None")]
    None
  }

}
