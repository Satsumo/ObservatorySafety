using Microsoft.Extensions.Logging;

using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Monitor
{
  public sealed class HeartbeatMonitor: StatusMonitorBase
  {
    private readonly ILogger<HeartbeatMonitor> _logger;

    public HeartbeatMonitor(ILogger<HeartbeatMonitor> logger) : base(60)
    {
      _logger = logger;
    }

    public override MonitorType MonitorType => MonitorType.Heartbeat;

    public override ILogger Logger => _logger;
    
    protected override void Poll()
    {
      // Note that we have the watchdog service that will monitor the log file for the safety service and it
      // will alert if there's no activity.
      // But activity is not guaranteed - if everything just ticks along without issues.  So, we log here
      // because it will then act as a heartbeat in the log files
      _logger.LogInformation("Checking power status");
    }
  }
}
