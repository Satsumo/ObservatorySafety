namespace ObservatorySafety.Core.Options
{
  public class HttpClientMonitorOptionsBase : MonitorOptionsBase
  {
    public string BaseUrl { get; set; } = "";

    public string? ApiKey { get; set; }
  }
}