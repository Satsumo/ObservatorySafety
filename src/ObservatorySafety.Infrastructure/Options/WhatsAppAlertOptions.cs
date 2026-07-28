namespace ObservatorySafety.Infrastructure.Options
{
  /*
   * "WhatsApp": {
        "Enabled": true,
        "TwilioSid": "YOUR_TWILIO_SID_HERE",
        "TwilioToken": "YOUR_TWILIO_TOKEN_HERE",
        "FromNumber": "whatsapp:+YOUR_TWILIO_WHATSAPP_NUMBER",
        "ToNumber": "whatsapp:+YOUR_PERSONAL_WHATSAPP_NUMBER"
      }
   */
  public class WhatsAppAlertOptions
  {
    public bool Enabled { get; set; } = false;

    public string TwilioSid { get; set; } = String.Empty;

    public string TwilioToken {  get; set; } = String.Empty;

    public string FromNumber { get; set; } = String.Empty;

    public string ToNumber { get; set; } = String.Empty;
  }
}
