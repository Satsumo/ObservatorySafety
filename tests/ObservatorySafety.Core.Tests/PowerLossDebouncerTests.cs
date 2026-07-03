using NUnit.Framework;

using ObservatorySafety.Core;

namespace ObservatorySafety.Core.Tests;

[TestFixture]
public class PowerLossDebouncerTests
{
  [Test]
  public async Task ConfirmsPowerLoss_AfterThreshold()
  {
    var debouncer = new PowerLossDebouncer(TimeSpan.FromMilliseconds(100));
    bool confirmed = false;

    debouncer.PowerLossConfirmed += (_, __) => confirmed = true;

    debouncer.OnStatusChanged(new PowerStatus(false, true));

    await Task.Delay(150);

    Assert.That(confirmed);
  }

  [Test]
  public async Task DoesNotConfirm_WhenPowerReturnsBeforeThreshold()
  {
    var debouncer = new PowerLossDebouncer(TimeSpan.FromMilliseconds(200));
    bool confirmed = false;

    debouncer.PowerLossConfirmed += (_, __) => confirmed = true;

    debouncer.OnStatusChanged(new PowerStatus(false, true));
    await Task.Delay(50);
    debouncer.OnStatusChanged(new PowerStatus(true, false));

    await Task.Delay(300);

    Assert.That(!confirmed);
  }

  [Test]
  public async Task MultiplePowerLossCycles_WorkCorrectly()
  {
    var debouncer = new PowerLossDebouncer(TimeSpan.FromMilliseconds(100));
    int count = 0;

    debouncer.PowerLossConfirmed += (_, __) => count++;

    // First cycle
    debouncer.OnStatusChanged(new PowerStatus(false, true));
    await Task.Delay(150);

    // Second cycle
    debouncer.OnStatusChanged(new PowerStatus(true, false));
    await Task.Delay(50);
    debouncer.OnStatusChanged(new PowerStatus(false, true));
    await Task.Delay(150);

    Assert.That(2, Is.EqualTo(count));
  }
}
