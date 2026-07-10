using NUnit.Framework;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Tests;

namespace ObservatorySafety.Watchdog.Tests
{
  [SetUpFixture]
  public class TestSetup
  {
    [OneTimeSetUp]
    public void Init()
    {
      LogProvider.Factory = TestLogging.CreateFactory();
    }
  }
}
