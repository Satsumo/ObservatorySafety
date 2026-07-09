using ObservatorySafety.Core;

public class PowerMonitorService : BackgroundService
{
  private readonly ILogger<PowerMonitorService> _logger = LogProvider.Factory!.CreateLogger<PowerMonitorService>();
  private readonly IPowerStatusProvider _powerStatusProvider;
  private readonly TimeSpan _powerlossConfirmedThreshold;

  public event EventHandler? PowerLost;

  private PowerStatus _lastStatus = PowerStatus.Online;

  public PowerMonitorService(IPowerStatusProvider powerStatusProvider, TimeSpan powerlossConfirmedThreshold)
  {
    _powerStatusProvider = powerStatusProvider;
    _powerlossConfirmedThreshold = powerlossConfirmedThreshold;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("PowerMonitorService started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      var current = _powerStatusProvider.GetPowerStatus();

      if (current != _lastStatus)
      {
        _logger.LogWarning("Power status changed: {Status}", current);
        _lastStatus = current;

        if (current == PowerStatus.OnBattery)
        {
          // Confirm outage for 30 seconds
          if (await ConfirmPowerLossAsync(stoppingToken))
          {
            PowerLost?.Invoke(this, EventArgs.Empty);
          }
        }
        else
        {
          _lastStatus = PowerStatus.Online;
        }
      }

      await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
    }

    _logger.LogInformation("PowerMonitorService stopped.");
  }

  private async Task<bool> ConfirmPowerLossAsync(CancellationToken token)
  {
    var start = DateTime.UtcNow;

    while (DateTime.UtcNow - start < _powerlossConfirmedThreshold)
    {
      if (token.IsCancellationRequested)
      {
        _logger.LogInformation("Cancellation requested hence power loss ignored.");
        return false;
      }

      var status = _powerStatusProvider.GetPowerStatus();

      if (status == PowerStatus.Online)
      {
        _logger.LogInformation("Power restored during confirmation window.");
        return false;
      }

      await Task.Delay(TimeSpan.FromSeconds(1), token);
    }

    _logger.LogWarning("Power loss confirmed after {Seconds} seconds.", _powerlossConfirmedThreshold.TotalSeconds);
    return true;
  }

}
