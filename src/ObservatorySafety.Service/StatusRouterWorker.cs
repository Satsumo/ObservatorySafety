
using ObservatorySafety.Core.Routing;

namespace ObservatorySafety.Infrastructure
{

  public sealed class StatusRouterWorker : BackgroundService
  {
    private readonly StatusRouter _router;
    private readonly ILogger<StatusRouterWorker> _logger;

    public StatusRouterWorker(ILogger<StatusRouterWorker> logger, StatusRouter router)
    {
      _router = router;
      _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation("StatusRouterWorker started.");

      _router.StartAllMonitors();

      while (!stoppingToken.IsCancellationRequested)
      {
        _router.Poll();
        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
      }

      _router.StopAllMonitors();
      _logger.LogInformation("StatusRouterWorker stopped.");
    }
  }

}
