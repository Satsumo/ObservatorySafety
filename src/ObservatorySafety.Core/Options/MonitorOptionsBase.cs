using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Core.Options
{
  public abstract class MonitorOptionsBase
  {
    public bool Enabled { get; set; }

    public int PollingPeriodSeconds { get; set; }

    public StatusType[] MonitoredStatuses { get; set; } = [];
  }
}