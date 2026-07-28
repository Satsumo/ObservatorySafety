namespace ObservatorySafety.Infrastructure.Options
{
  /*
   * "Pushover": {
       "Enabled": true,
       "UserKey": "YOUR_PUSHOVER_USER_KEY",
       "AppToken": "YOUR_PUSHOVER_APP_TOKEN_HERE"
     }
   */
  public class PushOverAlertOptions
  {
    public bool Enabled { get; set; } = false;

    public string UserKey { get; set; } = String.Empty;

    public string AppToken {  get; set; } = String.Empty;
  }
}
