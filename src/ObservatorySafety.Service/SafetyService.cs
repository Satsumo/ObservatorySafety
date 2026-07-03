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
  private readonly Serilog.ILogger _log;

  public SafetyService(
      StatusFileWatcher watcher,
      PowerLossDebouncer debouncer,
      ShutdownOrchestrator orchestrator,
      INinaClient nina,
      Serilog.ILogger log)
  {
    _watcher = watcher;
    _debouncer = debouncer;
    _orchestrator = orchestrator;
    _nina = nina;
    _log = log;

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
        await _orchestrator.ExecuteAsync(cmd, _nina);
      }
    };
  }

  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _log.Information("Observatory Safety Service started.");
    return Task.CompletedTask;
  }
}
