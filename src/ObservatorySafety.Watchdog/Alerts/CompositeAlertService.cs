using ObservatorySafety.Core;

namespace ObservatorySafety.Watchdog.Alerts
{
  public class CompositeAlertService : IAlertService
  {
    private ILogger<CompositeAlertService>? _loggerBase;
    private ILogger<CompositeAlertService> _logger =>
        _loggerBase ??= LogProvider.Factory!.CreateLogger<CompositeAlertService>();
        
    private readonly IConfiguration _configuration;
    private readonly IDictionary<string, IAlertService> _channels = new Dictionary<string, IAlertService>();

    public CompositeAlertService(IConfiguration configuration)
    {
      _configuration = configuration.GetSection("AlertChannels");
    }

    public void AddAlertService(string channelName, IAlertService alertService)
    {
      _channels[channelName] = alertService;
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      foreach (var channel in _channels)
      {
        if (_configuration.GetSection(channel.Key).GetValue<bool>("Enabled"))
        {
          try
          {
            await channel.Value.SendAlertAsync(title, message, cancellationToken);
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error sending alert via {ChannelName}", channel.Key);
          }
        }
      }
    }
  }
}
