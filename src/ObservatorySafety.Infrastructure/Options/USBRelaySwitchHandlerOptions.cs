using ObservatorySafety.Core.Helper;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Infrastructure.Configuration
{
  public class USBRelaySwitchHandlerOptions
  {
    public bool Enabled { get; set; }

    public string VendorID { get; set; } = "";

    public string ProductID { get; set; } = "";

    public int BaudRate { get; set; } = 9600;

    public RelayCommandType CommandType { get; set; } = RelayCommandType.Hex;

    public MonitorType[] StatusProviders { get; set; } = [];

    public StatusType[] Relays { get; set; } = [];
  }
}
