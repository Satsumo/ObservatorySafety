using NUnit.Framework;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class StatusFileWatcherTests
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
  public void EmitsCorrectStatus_OnFileCreateAndDelete()
  {
    var opts = new SafetyOptions { FlagFilePath = _flagFile };
    var watcher = new StatusFileWatcher(opts);

    PowerStatus? last = null;
    watcher.StatusChanged += (_, status) => last = status;

    // Create flag
    File.WriteAllText(_flagFile, "out");
    Thread.Sleep(50);

    Assert.That(last, Is.Not.Null);
    Assert.That(last!.IsOnGrid, Is.False);
    Assert.That(last.IsCritical);

    // Delete flag
    File.Delete(_flagFile);
    Thread.Sleep(50);

    Assert.That(last!.IsOnGrid);
    Assert.That(last.IsCritical, Is.False);
  }
}
