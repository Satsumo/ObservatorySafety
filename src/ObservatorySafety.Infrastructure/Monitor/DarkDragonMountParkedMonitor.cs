using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.Model;
using ObservatorySafety.Infrastructure.Options;

using System.Text.Json;

namespace ObservatorySafety.Infrastructure.Monitor
{

  public sealed class DarkDragonMountParkedMonitor : StatusMonitorBase
  {
    private readonly ILogger<DarkDragonMountParkedMonitor> _logger;
    private readonly DarkDragonOptions _options;

    private readonly IHttpService _httpService;

    public DarkDragonMountParkedMonitor(ILogger<DarkDragonMountParkedMonitor> logger, IOptions<DarkDragonOptions> options, IHttpService httpService) : base(options.Value.PollingPeriodSeconds)
    {
      _logger = logger;
      _options = options.Value;
      _httpService = httpService;
    }

    public override MonitorType MonitorType => MonitorType.DarkDragonMountSensor;

    public override ILogger Logger => _logger;

    protected override void Poll()
    {
      this.GetDarkDragonStatusAsync().ContinueWith(task =>
      {
        if (task.Result == null) 
        {
          // assume it is parked if we could not query the mount sensor
          this.Statuses = new Dictionary<StatusType, bool>{
            { StatusType.MountParked, true }
          };
        }
        else
        {
          this.Statuses = new Dictionary<StatusType, bool>{ 
            { StatusType.MountParked, task.Result.IsSafeToMove } 
          };
        }
      });
    }

    private async Task<DarkDragonModel?> GetDarkDragonStatusAsync() {
      try
      {
        var resp = await _httpService.Call(HttpMethod.Get, "status");
        var json = await resp.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<DarkDragonModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting dark dragon status!");
        return null;
      }
    }
  }

}
