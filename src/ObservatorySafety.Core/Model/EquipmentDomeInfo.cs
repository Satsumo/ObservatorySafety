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
