using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.Json;

using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.Model;
using ObservatorySafety.Infrastructure.Options;

namespace ObservatorySafety.Infrastructure.Monitor
{
  public sealed class CloudWatcherMonitor: StatusMonitorBase
  {
    private readonly ILogger<CloudWatcherMonitor> _logger;
    private readonly CloudWatcherOptions _options;

    public CloudWatcherMonitor(ILogger<CloudWatcherMonitor> logger, IOptions<CloudWatcherOptions> options) : base(options.Value.PollingPeriodSeconds)
    {
      _logger = logger;
      _options = options.Value;
    }

    public override MonitorType MonitorType => MonitorType.CloudWatcher;

    public override StatusType[] ProvidedStatuses => _options.MonitoredStatuses;

    public override ILogger Logger => _logger;
    
    protected override void Poll()
    {
      try 
      {
        var json = File.ReadAllText(_options.Path);
        var model = JsonSerializer.Deserialize<CloudWatcherModel>(json);

        this.Statuses = new Dictionary<StatusType, bool>{
          { StatusType.CloudWatcherDataStale, false }
        };
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Failed to get CloudWatcher data, hence assuming it is stale.");
        this.Statuses = new Dictionary<StatusType, bool>{
          { StatusType.CloudWatcherDataStale, true }
        };
      }
    }
  }
}
