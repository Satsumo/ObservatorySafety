using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ObservatorySafety.Watchdog.Alerts
{
    public class WhatsAppAlertService : IAlertService
    {
        private readonly string _sid;
        private readonly string _token;
        private readonly string _fromNumber;
        private readonly string _toNumber;
        private bool _initialized;

        public WhatsAppAlertService(IConfiguration configuration)
        {
            var section = configuration.GetSection("AlertChannels:WhatsApp");
            _sid = section.GetValue<string>("TwilioSid") ?? string.Empty;
            _token = section.GetValue<string>("TwilioToken") ?? string.Empty;
            _fromNumber = section.GetValue<string>("FromNumber") ?? string.Empty;
            _toNumber = section.GetValue<string>("ToNumber") ?? string.Empty;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            if (!string.IsNullOrWhiteSpace(_sid) && !string.IsNullOrWhiteSpace(_token))
            {
                TwilioClient.Init(_sid, _token);
                _initialized = true;
            }
        }

        public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
        {
            EnsureInitialized();

            if (!_initialized ||
                string.IsNullOrWhiteSpace(_fromNumber) ||
                string.IsNullOrWhiteSpace(_toNumber))
            {
                return;
            }

            var body = $"{title}: {message}";

            await MessageResource.CreateAsync(
                from: new Twilio.Types.PhoneNumber(_fromNumber),
                to: new Twilio.Types.PhoneNumber(_toNumber),
                body: body
            );
        }
    }
}
