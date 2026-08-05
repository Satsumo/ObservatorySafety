using ObservatorySafety.Core.Options;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Options
{
  public class ShutdownOptions
  {
    public bool Enabled { get; set; } = true;

    // Shutdown will happen this many minutes after trigger criteria is met (assuming criteria is still being met)
    public int ShutdownThresholdMinutes { get; set; } = 10;

    // A collection of statuses that trigger a shutdown.  If ALL of these are false then we trigger a shutdown.
    public StatusType[] ShutdownTriggerStatuses { get; set; } = [];

    // A collection of statuses we set to true to trigger a shutdown (typically "MountParked" and "RoofClosed").
    public StatusType[] ShutdownStatuses { get; set; } = [];

    // A collection of ASCOM devices that handle status changes we want to do for shutdown (typically the mount and the dome controller ASCOM devices)
    public AscomMonitorOptions[] AscomHandlers { get; set; } = [];

    public int DelayBetweenSwitchCallsSeconds { get; set; } = 10;

  }
}
