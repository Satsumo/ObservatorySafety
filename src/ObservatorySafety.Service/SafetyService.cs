using ObservatorySafety.Core;

namespace ObservatorySafety.Service;

public class SafetyService : BackgroundService
{
  private readonly ILogger<SafetyService> _logger = LogProvider.Factory!.CreateLogger<SafetyService>();

  private readonly PowerMonitorService _watcher;
  private readonly ShutdownOrchestrator _orchestrator;
  private readonly IAstronomyApplicationClient _astronomyApplicationClient;

  public SafetyService(
      PowerMonitorService watcher,
      ShutdownOrchestrator orchestrator,
      IAstronomyApplicationClient astronomyApplicationClient)
  {
    _watcher = watcher;
    _orchestrator = orchestrator;
    _astronomyApplicationClient = astronomyApplicationClient;

    _watcher.PowerLost += async (_, __) =>
    {
      var cmd = _orchestrator.GetCommandFor(PowerStatus.OnBattery);
      if (cmd != null)
      {
        _logger.Log(LogLevel.Information, "Executing shutdown command: {@Command}", cmd);
        await _astronomyApplicationClient.ExecuteShutdownAsync(cmd);
      }
    };
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.Log(LogLevel.Information, "SafetyService started.");
    await Task.CompletedTask;
  }
}
