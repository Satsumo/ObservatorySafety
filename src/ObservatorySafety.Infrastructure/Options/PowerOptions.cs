using ObservatorySafety.Core.Options;

namespace ObservatorySafety.Infrastructure.Options
{
  public class PowerOptions: MonitorOptionsBase
  {
    public int PowerOutageConfirmedThresholdSeconds { get; set; }
  }
}
