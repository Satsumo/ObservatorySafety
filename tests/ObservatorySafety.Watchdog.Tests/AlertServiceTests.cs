using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

using ObservatorySafety.Watchdog.Alerts;

namespace ObservatorySafety.Watchdog.Tests
{
    public class AlertServiceTests
    {
        [Test]
        public async Task CompositeAlertService_CallsEnabledChannels()
        {
            var settings = new[]
            {
                new KeyValuePair<string, string?>("AlertChannels:Pushover:Enabled", "true"),
                new KeyValuePair<string, string?>("AlertChannels:Email:Enabled", "true"),
                new KeyValuePair<string, string?>("AlertChannels:WhatsApp:Enabled", "false")
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            var pushover = new DummyChannel();
            var email = new DummyChannel();
            var whatsapp = new DummyChannel();

            var composite = new CompositeAlertService(NullLogger<CompositeAlertService>.Instance, configuration);
            composite.AddAlertService("Pushover", pushover);
            composite.AddAlertService("Email", email);
            composite.AddAlertService("WhatsApp", whatsapp);

            await composite.SendAlertAsync("Test", "Message", CancellationToken.None);

            Assert.That(pushover.Count, Is.EqualTo(1));
            Assert.That(email.Count, Is.EqualTo(1));
            Assert.That(whatsapp.Count, Is.EqualTo(0));
        }

        private class DummyChannel : IAlertService
        {
            public int Count { get; private set; }

            public Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
            {
                Count++;
                return Task.CompletedTask;
            }
        }
    }

}
