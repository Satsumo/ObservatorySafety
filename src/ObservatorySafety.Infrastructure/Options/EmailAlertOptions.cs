namespace ObservatorySafety.Infrastructure.Options
{
  /*
   * "Email": {
        "Enabled": true,
        "SmtpServer": "YOUR_EMAIL_SMTP_PROVIDER_URL",
        "SmtpPort": 587,
        "UseTls": true,
        "Username": "YOUR_EMAIL_ADDRESS",
        "Password": "YOUR_EMAIL_APP_PASSWORD_HERE",
        "From": "YOUR_EMAIL_ADDRESS",
        "To": "EMAIL_ADDRESS_YOU_WANT_ALERT_TO_GO_TO"
      },
   */
  public class EmailAlertOptions
  {
    public bool Enabled { get; set; } = false;

    public string SmtpServer { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool UseTls { get; set; } = false;

    public string UserName { get; set; } = String.Empty;

    public string Password { get; set; } = String.Empty;

    public string From { get; set; } = string.Empty;

    public string To { get; set; } = String.Empty;


  }
}
