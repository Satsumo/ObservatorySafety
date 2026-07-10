using ObservatorySafety.Core;

namespace ObservatorySafety.Watchdog.Alerts
{
  public class PushoverAlertService : IAlertService
  {
    private ILogger<PushoverAlertService>? _loggerBase;
    private ILogger<PushoverAlertService> _logger =>
        _loggerBase ??= LogProvider.Factory!.CreateLogger<PushoverAlertService>();

    private readonly HttpClient _httpClient;
    private readonly string _userKey;
    private readonly string _appToken;

    public PushoverAlertService(IConfiguration configuration)
    {
      _httpClient = new HttpClient();
      var section = configuration.GetSection("AlertChannels:Pushover");
      _userKey = section.GetValue<string>("UserKey") ?? string.Empty;
      _appToken = section.GetValue<string>("AppToken") ?? string.Empty;
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(_userKey) || string.IsNullOrWhiteSpace(_appToken))
        return;

      var content = new FormUrlEncodedContent(new[]
      {
                new KeyValuePair<string, string>("token", _appToken),
                new KeyValuePair<string, string>("user", _userKey),
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
