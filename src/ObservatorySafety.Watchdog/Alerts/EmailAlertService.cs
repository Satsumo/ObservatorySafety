using System.Net;
using System.Net.Mail;

namespace ObservatorySafety.Watchdog.Alerts
{
  public class EmailAlertService : IAlertService
  {
    private ILogger<EmailAlertService> _logger;

    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly bool _useTls;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;
    private readonly string _to;

    public EmailAlertService(ILogger<EmailAlertService> logger, IConfiguration configuration)
    {
      _logger = logger;
      var section = configuration.GetSection("AlertChannels:Email");
      _smtpServer = section.GetValue<string>("SmtpServer") ?? string.Empty;
      _smtpPort = section.GetValue<int>("SmtpPort");
      _useTls = section.GetValue<bool>("UseTls");
      _username = section.GetValue<string>("Username") ?? string.Empty;
      _password = section.GetValue<string>("Password") ?? string.Empty;
      _from = section.GetValue<string>("From") ?? string.Empty;
      _to = section.GetValue<string>("To") ?? string.Empty;
    }

    public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
    {
      if (string.IsNullOrWhiteSpace(_smtpServer) ||
          string.IsNullOrWhiteSpace(_username) ||
          string.IsNullOrWhiteSpace(_password) ||
          string.IsNullOrWhiteSpace(_from) ||
          string.IsNullOrWhiteSpace(_to))
      {
        return;
      }

      _logger.LogDebug("Sending email alert from {from} to {To} via SMTP server {SmtpServer}:{SmtpPort}.", _from, _to, _smtpServer, _smtpPort);
      using var client = new SmtpClient(_smtpServer, _smtpPort)
      {
        EnableSsl = _useTls,
        Credentials = new NetworkCredential(_username, _password)
      };

      using var mail = new MailMessage(_from, _to)
      {
        Subject = title,
        Body = message
      };

      await client.SendMailAsync(mail);
      _logger.LogDebug("Email alert sent successfully from {from} to {To}.", _from, _to);
    }
  }
}
