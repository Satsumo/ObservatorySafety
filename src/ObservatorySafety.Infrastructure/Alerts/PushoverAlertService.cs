
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Infrastructure.Options;


namespace ObservatorySafety.Infrastructure.Alerts
{
  public class PushoverAlertService : IAlertService
  {
    private readonly ILogger<PushoverAlertService> _logger;
    private readonly PushOverAlertOptions _options;

    private readonly HttpClient _httpClient;


    public PushoverAlertService(ILogger<PushoverAlertService> logger, IOptions<PushOverAlertOptions> options)
    {
      _logger = logger;
      _options = options.Value;
      _httpClient = new HttpClient();
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(_options.UserKey) || string.IsNullOrWhiteSpace(_options.AppToken))
      {
        _logger.LogWarning("PushOver options not correctly set hence unable to send PushOver alert");
        return;
      }

      var content = new FormUrlEncodedContent(new[]
      {
                new KeyValuePair<string, string>("token", _options.AppToken),
                new KeyValuePair<string, string>("user", _options.UserKey),
                new KeyValuePair<string, string>("title", title),
                new KeyValuePair<string, string>("message", message),
                new KeyValuePair<string, string>("priority", "1")
            });

      _logger.LogDebug("Sending Pushover alert with title: {Title} and message: {Message}", title, message);
      await _httpClient.PostAsync("https://api.pushover.net/1/messages.json", content, cancellationToken);
      _logger.LogDebug("Pushover alert sent successfully with title: {Title}", title);
    }
  }
}
