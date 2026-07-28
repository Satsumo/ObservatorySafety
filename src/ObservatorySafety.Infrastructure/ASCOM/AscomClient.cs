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

    public bool IsConnected => _switch.Connected;

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
      return _switch.GetSwitch(switchID);
    }

    public void SetSwitchValue(short switchID, bool value)
    {
      _switch.SetSwitch(switchID, value);
    }

    private Switch GetSwitch()
    {
      if (_switch == null)
      {
        _switch = new Switch(_progId)
        {
          Connected = true
        };
      }
      return _switch;
    }
  }
}
