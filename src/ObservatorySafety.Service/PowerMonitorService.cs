using Microsoft.Extensions.Hosting;

using Serilog;

using System.Windows.Forms; // required for SystemInformation

namespace ObservatorySafety.Service;

public class PowerMonitorService : BackgroundService
{
  private readonly string _flagFile;
  private readonly Serilog.ILogger _log;

  public PowerMonitorService(string flagFile, Serilog.ILogger log)
  {
    _flagFile = flagFile;
    _log = log;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _log.Information("PowerMonitorService started.");

    while (!stoppingToken.IsCancellationRequested)
    {
      var status = SystemInformation.PowerStatus.PowerLineStatus;

      if (status == PowerLineStatus.Offline)
      {
        if (!File.Exists(_flagFile))
        {
          _log.Warning("UPS reports mains power OFF — creating flag file.");
          File.WriteAllText(_flagFile, "power_out");
        }
      }
      else
      {
        if (File.Exists(_flagFile))
        {
          _log.Information("UPS reports mains power ON — deleting flag file.");
          File.Delete(_flagFile);
        }
      }

      await Task.Delay(1000, stoppingToken);
    }
  }
}
