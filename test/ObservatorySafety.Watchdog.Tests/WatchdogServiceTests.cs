using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;
using ObservatorySafety.Watchdog.Services;
using Xunit;

namespace ObservatorySafety.Watchdog.Tests
{
    public class WatchdogServiceTests
    {
        [Fact]
        public async Task WatchdogService_StartsAndStops()
        {
            var inMemorySettings = new[]
            {
                new KeyValuePair<string, string?>("Watchdog:MainServiceName", "DummyService"),
                new KeyValuePair<string, string?>("Watchdog:MainServiceLogPath", "C:\\Temp\\dummy.log"),
                new KeyValuePair<string, string?>("Watchdog:LogInactivityThresholdSeconds", "60"),
                new KeyValuePair<string, string?>("Watchdog:ServiceCheckIntervalSeconds", "1")
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var logger = new NullLogger<WatchdogService>();
            var logTailer = new LogTailer();
            var alertService = new DummyAlertService();

            var service = new WatchdogService(logger, configuration, logTailer, alertService);

            var cts = new CancellationTokenSource();
            var task = service.StartAsync(cts.Token);

            await Task.Delay(2000);
            await service.StopAsync(CancellationToken.None);
        }

        private class DummyAlertService : IAlertService
        {
            public Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
