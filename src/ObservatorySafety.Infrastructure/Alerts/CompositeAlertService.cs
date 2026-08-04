
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Infrastructure.Options;

using System.Text.RegularExpressions;

namespace ObservatorySafety.Infrastructure.Alerts
{
  public class CompositeAlertService : IAlertService
  {
    private ILogger<CompositeAlertService> _logger;
        
    private readonly IDictionary<string, IAlertService> _channels = new Dictionary<string, IAlertService>();

    private readonly Dictionary<string, DateTime> _lastAlertTimes = new();
    private readonly TimeSpan _alertSuppressionWindow;

    public CompositeAlertService(ILogger<CompositeAlertService> logger, IOptions<AlertOptions> alertOptions)
    {
      _logger = logger;
    
      var alertSuppressionDelay = alertOptions.Value.AlertSuppressionDelayMinutes;
      _alertSuppressionWindow = TimeSpan.FromMinutes(alertSuppressionDelay);
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
          var now = DateTime.UtcNow;
          var messageWithoutDateTime = Regex.Replace( message, @"\b\d{4}-\d{2}-\d{2}[ T]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?\b", "<DATETIME>").Trim();
          if (_lastAlertTimes.TryGetValue(messageWithoutDateTime, out var lastAlertTime))
          {
            if ((now - lastAlertTime) < _alertSuppressionWindow)
            {
              _logger.LogInformation("Alert '{Title}' suppressed due to suppression window.", title);
              return;
            }
          }
          _lastAlertTimes[messageWithoutDateTime] = now;
          await channel.Value.SendAlertAsync(title, message, cancellationToken);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error sending alert via {ChannelName}: {Message}", channel.Key, ex.Message);
        }
      }
    }

  }
}
