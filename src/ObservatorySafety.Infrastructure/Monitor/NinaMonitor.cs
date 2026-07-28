using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core.Status;
using ObservatorySafety.NINA;

namespace ObservatorSafety.NINA
{
  public class NinaMonitor : StatusMonitorBase
  {
    private readonly INinaClient _ninaClient;
    private readonly ILogger<NinaMonitor> _logger;
    private readonly NinaOptions _options;

    public NinaMonitor(INinaClient ninaClient, ILogger<NinaMonitor> logger, IOptions<NinaOptions> options)
      : base(options.Value.PollingPeriodSeconds)
    {
      _ninaClient = ninaClient;
      _logger = logger;
      _options = options.Value;
    }

    public override MonitorType MonitorType => MonitorType.NINA;

    protected override void Poll()
    {
      _ninaClient.GetEquipmentInfoAsync().ContinueWith(task =>
      {
        var equipmentInfo = task.IsCompletedSuccessfully ? task.Result?.Response : null;
        if (equipmentInfo == null)
        {
          _logger.LogWarning("NINA is not responding to equipment information requests, assuming application not running.");

          this.Statuses = new Dictionary<StatusType, bool>(){
            { StatusType.ApplicationRunning, false }
          };
        }
        else
        {

          _logger.LogInformation("NINA status data received: Weather safe: {WeatherSafe}, Dome closed: {DomeClosed}, Mount parked: {MountParked}, Sequence running: {SequenceRunning}",
            equipmentInfo.SafetyMonitor?.IsSafe, equipmentInfo.Dome?.ShutterStatus, equipmentInfo.Mount?.AtPark, equipmentInfo.Sequence?.IsRunning);

          IDictionary<StatusType, bool> statuses = new Dictionary<StatusType, bool>
          {
            { StatusType.ApplicationRunning, true }
          };

          if (equipmentInfo.Sequence != null)
          { 
            statuses.Add(StatusType.SequenceRunning, equipmentInfo.Sequence.IsRunning);
          }
          
          if (equipmentInfo.SafetyMonitor != null) 
          {
            statuses.Add(StatusType.WeatherSafe, equipmentInfo.SafetyMonitor.IsSafe);
          }

          if (equipmentInfo.Dome != null)
          {
            statuses.Add(StatusType.RoofClosed, equipmentInfo.Dome.ShutterStatus == "ShutterClosed");
          }

          if (equipmentInfo.Mount != null)
          {
            statuses.Add(StatusType.MountParked, equipmentInfo.Mount.AtPark);
          }

          this.Statuses = statuses;
        }
      });
    }
  }
}
