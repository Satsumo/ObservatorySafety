using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Infrastructure.Options;

using System.Net;
using System.Net.Mail;

namespace ObservatorySafety.Infrastructure.Alerts
{
  public class EmailAlertService : IAlertService
  {
    private readonly ILogger<EmailAlertService> _logger;

    private readonly EmailAlertOptions _options;

    public EmailAlertService(ILogger<EmailAlertService> logger, IOptions<EmailAlertOptions> options)
    {
      _logger = logger;
      _options = options.Value;
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(_options.SmtpServer) ||
          string.IsNullOrWhiteSpace(_options.UserName) ||
          string.IsNullOrWhiteSpace(_options.Password) ||
          string.IsNullOrWhiteSpace(_options.From) ||
          string.IsNullOrWhiteSpace(_options.To))
      {
        _logger.LogWarning("Email options are not configured correctly - unable to send Email alert.");
        return;
      }

      _logger.LogDebug("Sending email alert from {from} to {To} via SMTP server {SmtpServer}:{SmtpPort}.", 
        _options.From, _options.To, _options.SmtpServer, _options.SmtpPort);
      using var client = new SmtpClient(_options.SmtpServer, _options.SmtpPort)
      {
        EnableSsl = _options.UseTls,
        Credentials = new NetworkCredential(_options.UserName, _options.Password)
      };

      using var mail = new MailMessage(_options.From, _options.To)
      {
        Subject = title,
        Body = message
      };

      await client.SendMailAsync(mail);
      _logger.LogDebug("Email alert sent successfully from {from} to {To}.", _options.From, _options.To);
    }
  }
}
