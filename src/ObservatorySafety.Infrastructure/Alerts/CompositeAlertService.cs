
using Microsoft.Extensions.Logging;

namespace ObservatorySafety.Infrastructure.Alerts
{
  public class CompositeAlertService : IAlertService
  {
    private ILogger<CompositeAlertService> _logger;
        
    private readonly IDictionary<string, IAlertService> _channels = new Dictionary<string, IAlertService>();

    public CompositeAlertService(ILogger<CompositeAlertService> logger)
    {
      _logger = logger;
    }

    public void AddAlertService(string channelName, IAlertService alertService)
    {
      _channels[channelName] = alertService;
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      foreach (var channel in _channels)
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
