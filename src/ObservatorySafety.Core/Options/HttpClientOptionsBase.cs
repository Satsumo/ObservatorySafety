namespace ObservatorySafety.Core.Options
{
  public class HttpClientOptionsBase : MonitorOptionsBase
  {
    public string BaseUrl { get; set; } = "";

    public string? ApiKey { get; set; }
  }
}