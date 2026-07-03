using NUnit.Framework;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Tests;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Service;

using Serilog;
using Serilog.Core;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class SafetyServiceIntegrationTests
{
  private string _tempDir = null!;
  private string _flagFile = null!;

  [SetUp]
  public void Setup()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempDir);
    _flagFile = Path.Combine(_tempDir, "power_out.flag");
  }

  [TearDown]
  public void TearDown()
  {
    Directory.Delete(_tempDir, true);
  }

  [Test]
  public async Task SafetyService_TriggersShutdown_AfterDebounce()
  {
    var safetyOpts = new SafetyOptions
    {
      FlagFilePath = _flagFile,
      DebounceSeconds = 1
    };

    var watcher = new StatusFileWatcher(safetyOpts);
    var debouncer = new PowerLossDebouncer(TimeSpan.FromSeconds(1));
    var orchestrator = new ShutdownOrchestrator();
    var nina = new TestNinaClient();
    var log = new LoggerConfiguration().MinimumLevel.Debug().CreateLogger();

    var service = new SafetyService(watcher, debouncer, orchestrator, nina, log, true);

    // Simulate power loss
    File.WriteAllText(_flagFile, "out");
    await Task.Delay(1500);

    Assert.That(1, Is.EqualTo(nina.StopCount));
    Assert.That(1, Is.EqualTo(nina.ParkCount));
    Assert.That(1, Is.EqualTo(nina.WarmCount));
    Assert.That(1, Is.EqualTo(nina.CloseCount));
  }
}
