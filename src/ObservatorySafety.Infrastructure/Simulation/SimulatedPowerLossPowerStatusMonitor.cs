using Microsoft.Extensions.Logging;

using ObservatorySafety.Core.Abstractions;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Simulation
{
  public sealed class SimulatedPowerLossPowerStatusMonitor : StatusMonitorBase
  {
    private readonly ILogger<SimulatedPowerLossPowerStatusMonitor> _logger;
    private int _counter = 0;

    public override MonitorType MonitorType => MonitorType.PowerStatus;

    public SimulatedPowerLossPowerStatusMonitor(ILogger<SimulatedPowerLossPowerStatusMonitor> logger)
    {
      _logger = logger;
    }

    protected override void Poll()
    {
      _counter++;

      bool acOnline = _counter == 1;   // First call = AC Online
      bool onBattery = !acOnline;      // Subsequent calls = On Battery

      Statuses = new Dictionary<StatusType, bool>()
      {
        { StatusType.PowerOn, acOnline }
      };


      _logger.LogInformation(
          "SimulatedPowerLossPowerStatusMonitor: Poll #{PollCount}. Status = {StatusDetail}",
          _counter,
          acOnline
      );
    }
  }
}
