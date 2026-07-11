
using System.Management;

using Microsoft.Extensions.Logging;

using ObservatorySafety.Core;

namespace ObservatorySafety.Infrastructure
{
  public class WmiPowerStatusProvider : IPowerStatusProvider
  {
    private readonly ILogger<WmiPowerStatusProvider> _logger;

    public WmiPowerStatusProvider(ILogger<WmiPowerStatusProvider> logger)
    {
      _logger = logger;
    }

    /// Reads UPS / AC status using WMI (Win32_Battery).
    /// This works for Eaton UPS and any HID‑compliant UPS.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
    public PowerStatus GetPowerStatus()
    {
      try  /// <summary>

      {
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");

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

          if (status == 1) // Discharging = UPS active = AC lost
            return PowerStatus.OnBattery;
        }

        // If no battery objects exist, assume AC online
        return PowerStatus.Online;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to read UPS power status.");
        return PowerStatus.Online;
      }
    }
  }
}
