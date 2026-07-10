using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;
using ObservatorySafety.Watchdog.Services;

namespace ObservatorySafety.Watchdog.Tests
{
  public class WatchdogServiceTests
  {
    private WatchdogService CreateService(
        int serviceInterval,
        int logInterval,
        string logDirectory = "C:\\Temp",
        string logPattern = "*.log")
    {
      var settings = new[]
      {
                new KeyValuePair<string, string?>("Watchdog:MainServiceName", "DummyService"),
                new KeyValuePair<string, string?>("Watchdog:MainServiceLogDirectory", logDirectory),
                new KeyValuePair<string, string?>("Watchdog:MainServiceLogPattern", logPattern),
                new KeyValuePair<string, string?>("Watchdog:LogInactivityThresholdSeconds", "60"),
                new KeyValuePair<string, string?>("Watchdog:ServiceCheckIntervalSeconds", serviceInterval.ToString()),
                new KeyValuePair<string, string?>("Watchdog:LogCheckIntervalSeconds", logInterval.ToString())
            };

      var configuration = new ConfigurationBuilder()
          .AddInMemoryCollection(settings)
          .Build();

      var logTailer = new LogTailer();
      var alertService = new DummyAlertService();

      return new WatchdogService(configuration, logTailer, alertService);
    }

    [Test]
    public async Task WatchdogService_StartsAndStops_Cleanly()
    {
      var service = CreateService(serviceInterval: 1, logInterval: 1);

      var cts = new CancellationTokenSource();

      var startTask = service.StartAsync(cts.Token);

      await Task.Delay(1500);

      await service.StopAsync(CancellationToken.None);

      Assert.That(startTask.IsCompleted, Is.True);
    }

    [Test]
    public async Task WatchdogService_Disables_ServiceChecker_WhenIntervalIsZero()
    {
      var service = CreateService(serviceInterval: 0, logInterval: 1);

      var cts = new CancellationTokenSource();

      var startTask = service.StartAsync(cts.Token);

      await Task.Delay(1500);

      await service.StopAsync(CancellationToken.None);

      Assert.That(startTask.IsCompleted, Is.True);
    }

    [Test]
    public async Task WatchdogService_Disables_LogChecker_WhenIntervalIsZero()
    {
      var service = CreateService(serviceInterval: 1, logInterval: 0);

      var cts = new CancellationTokenSource();

      var startTask = service.StartAsync(cts.Token);

      await Task.Delay(1500);

      await service.StopAsync(CancellationToken.None);

      Assert.That(startTask.IsCompleted, Is.True);
    }

    [Test]
    public async Task WatchdogService_Idles_WhenBothIntervalsAreZero()
    {
      var service = CreateService(serviceInterval: 0, logInterval: 0);

      var cts = new CancellationTokenSource();

      var startTask = service.StartAsync(cts.Token);

      await Task.Delay(1000);

      await service.StopAsync(CancellationToken.None);

      Assert.That(startTask.IsCompleted, Is.True);
    }

    private class DummyAlertService : IAlertService
    {
      public Task SendAlertAsync(string title, string message, CancellationToken cancellationToken)
      {
        // No-op
        return Task.CompletedTask;
      }
    }
  }
}
