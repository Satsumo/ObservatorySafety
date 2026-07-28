namespace ObservatorySafety.Core.Options
{
  public class HttpClientOptionsBase
  {
    public string BaseUrl { get; set; } = "";
    public string? ApiKey { get; set; }
    public int PollingPeriodSeconds { get; set; }

  }

}
