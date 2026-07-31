namespace ObservatorySafety.Core.Options
{
  public class AscomMonitorOptions : MonitorOptionsBase
  {
    public string AscomID { get; set; } = "";

    public string[] SwitchNames { get; set; } = [];

  }
}