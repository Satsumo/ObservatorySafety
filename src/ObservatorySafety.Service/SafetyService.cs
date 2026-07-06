using ObservatorySafety.Core;

namespace ObservatorySafety.Service;

public class SafetyService : BackgroundService
{
  private readonly ILogger<SafetyService> _logger = LogProvider.Factory.CreateLogger<SafetyService>();

  private readonly PowerMonitorService _watcher;
  private readonly ShutdownOrchestrator _orchestrator;
  private readonly INinaClient _nina;
  private readonly bool _simulatePowerLoss;

  public SafetyService(
      PowerMonitorService watcher,
      ShutdownOrchestrator orchestrator,
      INinaClient nina,
      bool simulatePowerLoss)
  {
    _watcher = watcher;
    _orchestrator = orchestrator;
    _nina = nina;
    _simulatePowerLoss = simulatePowerLoss;

    _watcher.PowerLost += async (_, __) =>
    {
      _logger.Log(LogLevel.Warning, "Power loss confirmed.");
      var cmd = _orchestrator.GetCommandFor(PowerStatus.OnBattery);
      if (cmd != null)
      {
        _logger.Log(LogLevel.Information, "Executing shutdown command: {@Command}", cmd);
        await _nina.ExecuteShutdownAsync(cmd);
      }
    };
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.Log(LogLevel.Information, "SafetyService started.");

    if (_simulatePowerLoss)
    {
      _logger.Log(LogLevel.Warning, "Simulated power loss triggered.");
      var cmd = _orchestrator.GetCommandFor(PowerStatus.OnBattery);
      if (cmd != null)
        await _nina.ExecuteShutdownAsync(cmd);
    }

    await Task.CompletedTask;
  }
}
