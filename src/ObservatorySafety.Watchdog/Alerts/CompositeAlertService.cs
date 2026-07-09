namespace ObservatorySafety.Watchdog.Alerts
{
    public class CompositeAlertService : IAlertService
    {
        private readonly List<IAlertService> _channels = new();

        public CompositeAlertService(
            IConfiguration configuration,
            IAlertService pushover,
            IAlertService email,
            IAlertService whatsapp)
        {
            var alertConfig = configuration.GetSection("AlertChannels");

            if (alertConfig.GetSection("Pushover").GetValue<bool>("Enabled"))
            {
                _channels.Add(pushover);
            }

            if (alertConfig.GetSection("Email").GetValue<bool>("Enabled"))
            {
                _channels.Add(email);
            }

            if (alertConfig.GetSection("WhatsApp").GetValue<bool>("Enabled"))
            {
                _channels.Add(whatsapp);
            }
        }

        public async Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
        {
            foreach (var channel in _channels)
            {
                await channel.SendAlertAsync(title, message, cancellationToken);
            }
        }
    }
}
