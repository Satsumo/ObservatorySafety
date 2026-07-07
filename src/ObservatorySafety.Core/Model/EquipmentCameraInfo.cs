using System.Text.Json.Serialization;

namespace ObservatorySafety.Core.Model
{
  public class EquipmentCameraInfo
  {
    public bool CoolerOn { get; set; }

    [JsonConverter(typeof(NinaNanDoubleConverter))]
    public double? Temperature { get; set; }

    public bool AtTargetTemp { get; set; }

    public bool Connected { get; set; }
  }
}
