using NUnit.Framework;

using ObservatorySafety.Core;

namespace ObservatorySafety.Core.Tests;

[TestFixture]
public class ShutdownOrchestratorTests
{
  [Test]
  public void CriticalPowerLoss_ProducesFullShutdownCommand()
  {
    var orchestrator = new ShutdownOrchestrator();
    var status = PowerStatus.OnBattery;

    var cmd = orchestrator.GetCommandFor(status);

    Assert.That(cmd, Is.Not.Null);
    Assert.That(cmd!.StopSequence);
    Assert.That(cmd.ParkMount);
    Assert.That(cmd.WarmCamera);
    Assert.That(cmd.CloseDome);
  }

  [Test]
  public void NormalPower_ProducesNoCommand()
  {
    var orchestrator = new ShutdownOrchestrator();
    var status = PowerStatus.Online;

    var cmd = orchestrator.GetCommandFor(status);

    Assert.That(cmd, Is.Null);
  }
}
