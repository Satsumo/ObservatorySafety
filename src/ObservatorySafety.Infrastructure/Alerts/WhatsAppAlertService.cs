
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Infrastructure.Options;

using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ObservatorySafety.Infrastructure.Alerts
{
  public class WhatsAppAlertService : IAlertService
  {
    private readonly ILogger<WhatsAppAlertService> _logger;
    private readonly WhatsAppAlertOptions _options;

    private bool _initialized;

    public WhatsAppAlertService(ILogger<WhatsAppAlertService> logger, IOptions<WhatsAppAlertOptions> options)
    {
      _logger = logger;
      _options = options.Value;
    }

    private void EnsureInitialized()
    {
      if (_initialized)
        return;

      if (!string.IsNullOrWhiteSpace(_options.TwilioSid) && !string.IsNullOrWhiteSpace(_options.TwilioToken))
      {
        try
        {
          TwilioClient.Init(_options.TwilioSid, _options.TwilioToken);
          _initialized = true;
          _logger.LogInformation("WhatsAppAlertService initialized with Twilio SID: {Sid}", _options.TwilioSid);
        }
        catch (Exception ex)
        {
          _logger.LogWarning("Unable to initialise WhatsAppAlertService. Check configuration.  Twilio SID: {Sid}", _options.TwilioSid);
        }
      }
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      EnsureInitialized();

      if (!_initialized ||
          string.IsNullOrWhiteSpace(_options.FromNumber) ||
          string.IsNullOrWhiteSpace(_options.ToNumber))
      {
        _logger.LogWarning("WhatsAppAlertService configuration is missing either the from or to number. Alert not sent.");
        return;
      }

      var body = $"{title}: {message}";

      await MessageResource.CreateAsync(
          from: new Twilio.Types.PhoneNumber(_options.FromNumber),
          to: new Twilio.Types.PhoneNumber(_options.ToNumber),
          body: body
      );

      _logger.LogInformation("WhatsApp alert sent to {ToNumber}: {Body}", _options.ToNumber, body);
    }
  }
}
