namespace ObservatorySafety.Infrastructure.Model
{
  /*
   * Handles CloudWatcher aag_json.dat file:   
      {
        "dateLocalTime" : "2026/07/30 17:35:35",
        "cwinfo" : "Serial: 3332, FW: 5.89",
        "slddata" : "2026-07-30 17:35:35.35 C K   -8.0   17.9   17.9    6.3  61   10.2   0 0 0 00003 046233.73304 1 2 1 2 0 0  +9.72",
        "clouds" : -8.0,
        "cloudsSafe" : "Safe",
        "temp" : 17.9,
        "wind" : 6.3,
        "windSafe" : "Safe",
        "gust" : 12.0,
        "rain" : 3200,
        "rainSafe" : "Safe",
        "lightmpsas" : 9.72,
        "lightSafe" : "Safe",
        "switch" : 1,
        "safe" : 1,
        "hum" : 60.767,
        "humSafe" : "Safe",
        "dewp" : 10.202,
        "abspress" : 996.250, 
        "relpress" : 1008.003, 
        "pressureSafe" : "Safe",
        "rawir" : -1.870
      }
   */
  public class CloudWatcherModel
  {
    public string DateLocalTime { get; set; } = string.Empty;
    public string CwInfo { get; set; } = string.Empty;
    public string SldData { get; set; } = string.Empty;

    public double Clouds { get; set; }
    public string CloudsSafe { get; set; } = string.Empty;

    public double Temp { get; set; }

    public double Wind { get; set; }
    public string WindSafe { get; set; } = string.Empty;

    public double Gust { get; set; }

    public int Rain { get; set; }
    public string RainSafe { get; set; } = string.Empty;

    public double LightMpsas { get; set; }
    public string LightSafe { get; set; } = string.Empty;

    public int Switch { get; set; }

    // safe = 1 or 0 in JSON → map to bool
    public int Safe { get; set; }

    public double Hum { get; set; }
    public string HumSafe { get; set; } = string.Empty;

    public double DewP { get; set; }

    public double AbsPress { get; set; }
    public double RelPress { get; set; }
    public string PressureSafe { get; set; } = string.Empty;

    public double RawIR { get; set; }
  }
}
