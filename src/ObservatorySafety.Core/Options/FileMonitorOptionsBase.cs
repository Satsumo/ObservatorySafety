namespace ObservatorySafety.Core.Options
{
  public abstract class FileMonitorOptionsBase : MonitorOptionsBase
  {
    public string Path { get; set; } = "";
  }
}