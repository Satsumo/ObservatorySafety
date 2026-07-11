
namespace ObservatorySafety.Service
{
  public class SafetyHeartbeatService : BackgroundService
  {
    private readonly ILogger<SafetyHeartbeatService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public SafetyHeartbeatService(ILogger<SafetyHeartbeatService> logger)
    {
      _logger = logger;
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
