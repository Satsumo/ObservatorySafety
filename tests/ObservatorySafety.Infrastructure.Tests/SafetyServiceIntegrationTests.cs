using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure.Simulation;
using ObservatorySafety.Service;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class SafetyServiceIntegrationTests
{
  [Test]
  public async Task SafetyService_TriggersShutdown_AfterDebounce()
  {
    
    var safetyOpts = new SafetyOptions
    {
      PowerOutageConfirmedThresholdSeconds = 1
    };

    var mockPowerStatusProvider = new SimulatedPowerLossPowerStatusProvider(NullLogger<SimulatedPowerLossPowerStatusProvider>.Instance);
    var watcher = new PowerMonitorService(NullLogger<PowerMonitorService>.Instance, mockPowerStatusProvider, TimeSpan.FromSeconds(safetyOpts.PowerOutageConfirmedThresholdSeconds));
    var orchestrator = new ShutdownOrchestrator();
    var nina = new SimulatedClient(NullLogger<SimulatedClient>.Instance);

    // IMPORTANT: disable auto-start
    var service = new SafetyService(NullLogger<SafetyService>.Instance, watcher, orchestrator, nina);

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
