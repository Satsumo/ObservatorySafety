using System.IO.Ports;
using System.Management;

namespace ObservatorySafety.Core.Helper
{
  public class PortHelper
  {
    public static string? FindUsbRelayPort(string vendorID, string productID)
    {
      var searcher = new ManagementObjectSearcher(
          "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

      foreach (var device in searcher.Get())
      {
        var name = (string) device["Name"];
        var deviceId = (string) device["DeviceID"];

        // Example: CH340 relay
        if (deviceId.Contains(vendorID) && deviceId.Contains(productID))
        {
          // Extract COM port from name
          var start = name.IndexOf("(COM") + 1;
          var end = name.IndexOf(")", start);
          return name.Substring(start, end - start);
        }
      }

      return null;
    }

    public static void SendRelayCommand(RelayCommandType commandType, SerialPort port, int relayNumber, bool value)
    {
      switch (commandType)
      {
        case RelayCommandType.Ascii:
          SendASCIIRelayCommand(port, relayNumber, value);
          break;

        case RelayCommandType.Hex:
          SendByteRelayCommand(port, relayNumber, value);
          break;

        default:
          throw new NotImplementedException($"Unsupport relay command type {commandType}");
      }
    }

    private static void SendByteRelayCommand(SerialPort port, int relayNumber, bool value)
    {
      byte header = 0xA0;
      byte relay = (byte) relayNumber;
      byte state = value ? (byte) 0x01 : (byte) 0x00;
      byte checksum = (byte) (header + relay + state);

      byte[] cmd = new[] { header, relay, state, checksum };
      port.Write(cmd, 0, cmd.Length);
    }

    private static void SendASCIIRelayCommand(SerialPort port, int relayNumber, bool value)
    {
      var relayValue = value ? 1 : 0;
      var cmd = $"AT+CH{relayNumber}={relayValue}";
      port.WriteLine(cmd);

    }

  }
}
