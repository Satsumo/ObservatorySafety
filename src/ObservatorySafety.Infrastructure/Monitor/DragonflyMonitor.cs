using System.Management;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.ASCOM;
using ObservatorySafety.Infrastructure.Options;

namespace ObservatorySafety.Infrastructure.Monitor
{
  public sealed class DragomflyMonitor : StatusMonitorBase
  {
    private readonly ILogger<DragomflyMonitor> _logger;
    private readonly DragonflyOptions _options;
    private readonly IAscomClient _ascomClient;

    public DragomflyMonitor(ILogger<DragomflyMonitor> logger, IOptions<DragonflyOptions> options, IAscomClient ascomClient) : base(options.Value.PollingPeriodSeconds)
    {
      _logger = logger;
      _options = options.Value;
      _ascomClient = ascomClient;
    }

    public override MonitorType MonitorType => MonitorType.Dragonfly;

    public override StatusType[] ProvidedStatuses => _options.MonitoredStatuses;

    public override ILogger Logger => _logger;

    protected override void Poll()
    {
      var statuses = new Dictionary<StatusType, bool>();

      for (short statusNumber = 0; statusNumber < _options.SensorStatusTypes.Length; statusNumber++)
      {
        var statusType = _options.SensorStatusTypes[statusNumber];
        var switchNumber = statusNumber + 1; //switches count from one, not zero

        try
        {
          var switchValue = _ascomClient.GetSwitchValue((short) switchNumber);
          statuses.Add(statusType, switchValue);
        }
        catch (Exception ex) 
        {
          _logger.LogError(ex, "Failed to get Dragonfly value for switch: {switchNumber}", switchNumber);
        }
      }

      this.Statuses = statuses;
    }
  }
}
