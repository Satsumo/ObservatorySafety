namespace ObservatorySafety.Core.Options
{
  public class AscomOptionsBase
  {
    public string AscomID { get; set; } = "";

    public int PollingPeriodSeconds { get; set; } = 30;

    public bool Enabled { get; set; } = true;
  }
}