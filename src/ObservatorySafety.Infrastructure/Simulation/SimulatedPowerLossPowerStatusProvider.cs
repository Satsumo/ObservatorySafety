using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure.Simulation
{
  public class SimulatedPowerLossPowerStatusProvider : IPowerStatusProvider
  {
    private readonly ILogger<SimulatedPowerLossPowerStatusProvider> _logger = LogProvider.Factory!.CreateLogger<SimulatedPowerLossPowerStatusProvider>();

    private int _counter = 0;

    public PowerStatus GetPowerStatus()
    {
      _counter++;

      var powerStatus = _counter == 1 ? PowerStatus.Online : PowerStatus.OnBattery;
      _logger.LogInformation("SimulatedPowerLossPowerStatusProvider: GetPowerStatus called {CallCount} times. Status is {powerStatus}", _counter, powerStatus);
      return powerStatus;
    }
  }
}
