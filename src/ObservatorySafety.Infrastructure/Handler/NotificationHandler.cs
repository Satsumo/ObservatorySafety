using Microsoft.Extensions.Logging;

using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.Alerts;

namespace ObservatorySafety.Infrastructure.Handler
{
  public class NotificationHandler : StatusHandlerBase
  {
    private readonly ILogger<NotificationHandler> _logger;
    private readonly IAlertService _alertService;

    private IDictionary<StatusType, bool> _previousAlertValues = new Dictionary<StatusType, bool>();

    public NotificationHandler(ILogger<NotificationHandler> logger, IAlertService alertService)
    {
      _logger = logger;
      _alertService = alertService;

      Config = new StatusHandlerConfig()
      {
        NotificationType = StatusNotificationType.OnChange
      };
    }

    public override string Name => "Notification";

    public override StatusHandlerConfig Config { get; }

    public override void HandleMonitorStates(StatusPacket packet)
    {
      // set up current state - assuming everything is good
      var currentState = new Dictionary<StatusType, bool>()
      {
        { StatusType.RoofOpen, false},
        { StatusType.RoofClosed, true},
        { StatusType.PowerOn, true},
        { StatusType.ApplicationRunning, true},
        { StatusType.WeatherSafe, true},
        { StatusType.MountParked, true}
      };

      foreach (var monitorState in packet.MonitorStates)
      {
        _logger.LogInformation("Monitor: {MonitorName}, State: {MonitorState}", monitorState.Key, monitorState.Value.Statuses);
        switch (monitorState.Key) {
          case MonitorType.PowerStatus:
            currentState[StatusType.PowerOn] = monitorState.Value.Statuses[StatusType.PowerOn];
            break;

          case MonitorType.NINA:
            currentState[StatusType.ApplicationRunning] = monitorState.Value.Statuses[StatusType.ApplicationRunning];
            break;

          case MonitorType.Dragonfly:
            currentState[StatusType.RoofOpen] = monitorState.Value.Statuses[StatusType.RoofOpen];
            currentState[StatusType.RoofClosed] = monitorState.Value.Statuses[StatusType.RoofClosed];
            currentState[StatusType.WeatherSafe] = monitorState.Value.Statuses[StatusType.WeatherSafe];
            break;

          case MonitorType.DarkDragonMountSensor:
            currentState[StatusType.MountParked] = monitorState.Value.Statuses[StatusType.MountParked];
            break;
        }
      }

      this.HandleMonitorStates(currentState);
    }

    public override void Dispose()
    {
      // Nothing to do
    }

    private void HandleMonitorStates(IDictionary<StatusType, bool> currentState)
    {
      var thereIsAProblem = false;
      
      // In our setup we expect power to be always on and for NINA to be always running.
      // If either of these is not true then if we send an alert.  And if in the alert we also
      // say that the roof is not closed then I'll no I have a problem to resolve.
      if (!currentState[StatusType.PowerOn])
      {
        thereIsAProblem = true;
      }

      if (!currentState[StatusType.ApplicationRunning]) {
        thereIsAProblem = true;
      }

      if (thereIsAProblem)
      {
        // We only send an alert if the alert values are different from the last sent alert
        var statusSameSinceLastAlert = _previousAlertValues.OrderBy(k => k.Key).SequenceEqual(currentState.OrderBy(k => k.Key));

        if (!statusSameSinceLastAlert)
        {
          _previousAlertValues = currentState;
          var message = "There is a problem.  The observatory states are as follows:\n\r" + 
            string.Join("\r\n", currentState.Select(kvp => $"{kvp.Key}={kvp.Value}"));

          _logger.LogWarning(message);

          _alertService.SendAlertAsync("Observatory Alert", message, CancellationToken.None);
        }
        else
        {
          _logger.LogInformation("Previously alerted on the same problem hence not sending alert notification");
        }
      }
      else
      {
        // No longer a problem so clear the last alert so that if an identical situation
        // occurs we will send an alert
        _previousAlertValues.Clear();

        _logger.LogInformation("No problems for notification service to alert on");
      }

    }
  }
}
