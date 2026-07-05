using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Extensions.Logging;

namespace ObservatorySafety.Core.Tests
{


  public static class TestLogging
  {
    public static ILoggerFactory CreateFactory()
    {
      var serilogLogger = new LoggerConfiguration()
          .MinimumLevel.Debug()
          .WriteTo.Console()
          .CreateLogger();

      return new SerilogLoggerFactory(serilogLogger, dispose: true);
    }
  }

}
