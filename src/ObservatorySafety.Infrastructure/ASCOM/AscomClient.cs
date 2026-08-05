using ASCOM.DriverAccess;

using Microsoft.Extensions.Logging;

namespace ObservatorySafety.Infrastructure.ASCOM
{
  public class AscomClient : IAscomClient, IDisposable
  {
    private readonly ILogger<AscomClient> _logger;
    private readonly string _progId;

    private Switch? _switch = null;

    public AscomClient(ILogger<AscomClient> logger, string progId)
    {
      _logger = logger;
      _progId = progId;

    }

    public bool IsConnected => _switch?.Connected ?? false;

    public void Dispose()
    {
      if (_switch != null)
      {
        _switch.Disconnect();
        _switch.Dispose();
        _switch = null;
      }
    }

    public bool GetSwitchValue(short switchID)
    {
      return this.GetAscomSwitch()?.GetSwitch(switchID) ?? false;
    }

    public void SetSwitchValue(short switchID, bool value)
    {
      this.GetAscomSwitch()?.SetSwitch(switchID, value);
    }

    public string GetSwitchName(short switchId)
    {
      return this.GetAscomSwitch()?.GetSwitchName(switchId) ?? string.Empty;
    }

    public short MaxSwitch => this.GetAscomSwitch()?.MaxSwitch ?? 0;

    private Switch? GetAscomSwitch()
    {
      if (_switch == null)
      {
        try
        {
          _switch = new Switch(_progId)
          {
            Connected = true
          };
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to connect to ASCOM device with ProgID: {ProgId}", _progId);
          return null;
        }
      }
      return _switch;
    }
  }
}
