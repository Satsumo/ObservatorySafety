using Microsoft.Extensions.Logging;

using Moq;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure.Tests.Mock;
using ObservatorySafety.Service;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class SafetyServiceIntegrationTests
{
  private ILogger<SafetyServiceIntegrationTests> _logger;

  [SetUp]
  public void Setup()
  {
    var loggerFactory = LoggerFactory.Create(builder =>
    {
      builder
          .SetMinimumLevel(LogLevel.Debug)
          .AddConsole();   // logs appear in test output
    });

    // Make your LogProvider use this factory
    LogProvider.Factory = loggerFactory;
    _logger = LogProvider.Factory.CreateLogger<SafetyServiceIntegrationTests>();

  }

  [TearDown]
  public void TearDown()
  {
  }

  [Test]
  public async Task SafetyService_TriggersShutdown_AfterDebounce()
  {
    
    var safetyOpts = new SafetyOptions
    {
      PowerOutageConfirmedThresholdSeconds = 1
    };

    var mockPowerStatusProvider = new Mock<IPowerStatusProvider>();
    
    var callCount = 0;
    mockPowerStatusProvider
        .Setup(p => p.GetPowerStatus())
        .Returns(() =>
        {
          callCount++;
          
          var powerStatus = callCount == 1 ? PowerStatus.Online : PowerStatus.OnBattery;
          _logger.LogInformation("GetPowerStatus called {CallCount} times. Status is {powerStatus}", callCount, powerStatus);
          
          return powerStatus;
        });

    var watcher = new PowerMonitorService(mockPowerStatusProvider.Object, TimeSpan.FromSeconds(safetyOpts.PowerOutageConfirmedThresholdSeconds));
    var orchestrator = new ShutdownOrchestrator();
    var nina = new MockNinaClient();

    // IMPORTANT: disable auto-start
    var service = new SafetyService(watcher, orchestrator, nina, false);

    // Start the background service
    await watcher.StartAsync(CancellationToken.None);

    // Allow time for:
    // - first poll
    // - debounce window
    // - second poll
    // - event propagation
    await Task.Delay(5000);

    Assert.That(nina.StopSequenceCount, Is.EqualTo(1));
    Assert.That(nina.ParkCount, Is.EqualTo(1));
    Assert.That(nina.WarmCount, Is.EqualTo(1));
    Assert.That(nina.CloseCount, Is.EqualTo(1));

    Assert.That(nina.CallLog,
        Is.EqualTo(new[] { "StopSequence", "ParkMount", "WarmCamera", "CloseDome" }).AsCollection);
  }

}
