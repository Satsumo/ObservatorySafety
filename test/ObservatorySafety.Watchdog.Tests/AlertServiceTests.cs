using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ObservatorySafety.Watchdog.Alerts;
using Xunit;

namespace ObservatorySafety.Watchdog.Tests
{
    public class AlertServiceTests
    {
        [Fact]
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

            var composite = new CompositeAlertService(configuration, pushover, email, whatsapp);

            await composite.SendAlertAsync("Test", "Message", CancellationToken.None);

            Assert.Equal(1, pushover.Count);
            Assert.Equal(1, email.Count);
            Assert.Equal(0, whatsapp.Count);
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
