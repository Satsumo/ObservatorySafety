using ObservatorySafety.Core.Options;

namespace ObservatorySafety.Infrastructure.Options
{
  public class CloudWatcherOptions: FileMonitorOptionsBase
  {
    public int StaleDataThresholdSeconds { get; set; } = 300;
  }
}
