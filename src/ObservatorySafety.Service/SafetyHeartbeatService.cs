
using ObservatorySafety.Core;

namespace ObservatorySafety.Service
{
  public class SafetyHeartbeatService : BackgroundService
  {
    private ILogger<SafetyHeartbeatService>? _loggerBase;
    private ILogger<SafetyHeartbeatService> _logger =>
        _loggerBase ??= LogProvider.Factory!.CreateLogger<SafetyHeartbeatService>();

    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public SafetyHeartbeatService()
    {
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("SafetyHeartbeatService started (interval: 1 minute)");

      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          _logger.LogInformation("SafetyService heartbeat OK — {Timestamp}", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error while writing SafetyService heartbeat");
        }

        await Task.Delay(_interval, stoppingToken);
      }

      _logger.LogInformation("SafetyHeartbeatService stopping");
    }
  }
}
