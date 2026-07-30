using System.Diagnostics;
using System.Management;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Monitor
{
  public sealed class WmiPowerStatusMonitor : StatusMonitorBase
  {
    private readonly ILogger<WmiPowerStatusMonitor> _logger;
    private readonly EquipmentOptions _options;

    private readonly Stopwatch _powerOutageTimer = new();

    public WmiPowerStatusMonitor(ILogger<WmiPowerStatusMonitor> logger, IOptions<EquipmentOptions> options) : base(options.Value.PowerOutagePollingTimeSeconds)
    {
      _logger = logger;
      _options = options.Value;
    }

    public override MonitorType MonitorType => MonitorType.PowerStatus;

    public override ILogger Logger => _logger;
    
    protected override void Poll()
    {
      try
      {
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");

        bool currentPowerState = this.Statuses.ContainsKey(StatusType.PowerOn) ? this.Statuses[StatusType.PowerOn] : true;
        bool newPowerState = true;

        bool powerOffConfirmed = false;

        foreach (var battery in searcher.Get())
        {
          if (battery["BatteryStatus"] == null)
            continue;

          var status = (UInt16) battery["BatteryStatus"];

          // BatteryStatus values:
          // 1 = Discharging (UPS running)
          // 2 = AC online
          // 3 = Fully Charged
          // 4 = Low
          // 5 = Critical
          // 6 = Charging
          // 7 = Charging and High
          // 8 = Charging and Low
          // 9 = Charging and Critical
          // 10 = Undefined
          // 11 = Partially Charged

          if (status == 1)
          {
            newPowerState = false;
            if (!currentPowerState)
            {
              // Keep reporting it as out
              _logger.LogInformation("Power is still out");
              powerOffConfirmed = true;
            }
            else
            { 
              // The power has just gone out - however, we do not report it as a power outage until a set
              // timer has elaspsed.
              if (!_powerOutageTimer.IsRunning)
              {
                _logger.LogInformation("Power outage detected - starting time to confirm whether power out for {_options.PowerOutageConfirmedThresholdSeconds} seconds.",
                                       _options.PowerOutageConfirmedThresholdSeconds);
                _powerOutageTimer.Start();
              }
              else if (_powerOutageTimer.Elapsed > TimeSpan.FromSeconds(_options.PowerOutageConfirmedThresholdSeconds))
              {
                _logger.LogWarning("Power outage confirmed.");
                powerOffConfirmed = true;
              }
            }
            break;
          }
        }

        if (!powerOffConfirmed)
        {
          // Power off is not confirmed - but if we now have detected power then we need to ensure
          // power off confirmation time is stopped and reset.
          if (newPowerState && _powerOutageTimer.IsRunning)
          {
            _logger.LogInformation("Power returned - stopping and resetting power outage timer.");
            _powerOutageTimer.Stop();
            _powerOutageTimer.Reset();
          }

          // We haven't confirmed power off hence ensure new power state is "on"
          newPowerState = true; 
        }

        this.Statuses = new Dictionary<StatusType, bool>{
          { StatusType.PowerOn, newPowerState }          
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to read UPS power status.");
        this.Statuses = new Dictionary<StatusType, bool>{
          { StatusType.PowerOn, true }
        };
      }
    }
  }
}
