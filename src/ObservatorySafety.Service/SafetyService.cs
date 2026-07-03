using Microsoft.Extensions.Hosting;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;

using Serilog;

namespace ObservatorySafety.Service;

public class SafetyService : BackgroundService
{
  private readonly StatusFileWatcher _watcher;
  private readonly PowerLossDebouncer _debouncer;
  private readonly ShutdownOrchestrator _orchestrator;
  private readonly INinaClient _nina;
  private readonly ILogger _log;
  private readonly bool _simulatePowerLoss;

  public SafetyService(
      StatusFileWatcher watcher,
      PowerLossDebouncer debouncer,
      ShutdownOrchestrator orchestrator,
      INinaClient nina,
      ILogger log,
      bool simulatePowerLoss)
  {
    _watcher = watcher;
    _debouncer = debouncer;
    _orchestrator = orchestrator;
    _nina = nina;
    _log = log;
    _simulatePowerLoss = simulatePowerLoss;

    _watcher.StatusChanged += (_, status) =>
    {
      _log.Information("Power status changed: {@Status}", status);
      _debouncer.OnStatusChanged(status);
    };

    _debouncer.PowerLossConfirmed += async (_, __) =>
    {
      _log.Warning("Power loss confirmed after debounce threshold.");
      var cmd = _orchestrator.GetCommandFor(new PowerStatus(false, true));
      if (cmd != null)
      {
        _log.Information("Executing shutdown command: {@Command}", cmd);
        await _nina.ExecuteShutdownAsync(cmd);
      }
    };
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _log.Information("SafetyService started.");

    if (_simulatePowerLoss)
    {
      _log.Warning("Simulated power loss triggered.");
      var cmd = _orchestrator.GetCommandFor(new PowerStatus(false, true));
      if (cmd != null)
        await _nina.ExecuteShutdownAsync(cmd);
    }

    await Task.CompletedTask;
  }
}
