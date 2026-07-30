using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core.Helper;
using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.Configuration;

using System.IO.Ports;

namespace ObservatorySafety.Infrastructure.Handler
{
  public sealed class USBRelaySwitchHandler : StatusHandlerBase
  {
    private readonly ILogger<USBRelaySwitchHandler> _logger;
    private readonly USBRelaySwitchHandlerOptions _options;
    private readonly SerialPort? _port;

    public USBRelaySwitchHandler(ILogger<USBRelaySwitchHandler> logger, IOptions<USBRelaySwitchHandlerOptions> options)
    {
      _logger = logger;
      _options = options.Value;

      Config = new StatusHandlerConfig()
      {
        NotificationType = StatusNotificationType.OnChange
      };

      var portName = PortHelper.FindUsbRelayPort(_options.VendorID, _options.ProductID);
      if (portName == null)
      {
        _logger.LogWarning("Unable to find port for Vendor {_options.VendorID} and Product {_options.ProductID}", _options.VendorID, _options.ProductID);
        _port = null;
      }
      else
      {
        _logger.LogInformation("Opening port '{portName}' for USB Relay Switch Handler", portName);
        _port = new SerialPort(portName, _options.BaudRate)
        {
          Parity = Parity.None,
          DataBits = 8,
          StopBits = StopBits.One,
          Handshake = Handshake.None,
          DtrEnable = true,
          RtsEnable = true,
          NewLine = "\r\n"
        };

        _port.Open();
        if (!_port.IsOpen)
        {
          _logger.LogWarning("Port is NOT opened!");
        }
      }
    }

    public override string Name => "USBRelaySwitch";

    public override StatusHandlerConfig Config { get; }

    public override async void HandleMonitorStates(StatusPacket packet)
    {
      if (!_options.Enabled)
        return;

      var relayValues = new Dictionary<int, bool?>();

      // Several monitors are involved in creating the status packed. The channel data may be spread across them, and sometimes
      // the data can come from multiple status providers.
      // The options contain StatusProviders in order of preference.  Once we get a channel's data we no longer look for it from
      // other providers.
      for (int provider = 0; provider < _options.StatusProviders.Length; provider++)
      {
        var monitor = packet.MonitorStates.ContainsKey(_options.StatusProviders[provider]) ? packet.MonitorStates[_options.StatusProviders[provider]] : null;
        if (monitor != null)
        {
          for (int channel = 0; channel < _options.Relays.Length; channel++)
          {
            var relayNumber = channel + 1; // relays start from ONE, not ZERO
            if (!relayValues.ContainsKey(relayNumber))
            {
              var channelName = _options.Relays[channel];
              if (monitor.Statuses.ContainsKey(channelName))
              {
                relayValues[relayNumber] = monitor.Statuses[channelName];
              }
            }
          }
        }
      }

      var relaysAsString = string.Join("\r\n", relayValues.Select(kvp => $"{kvp.Key}={kvp.Value}"));
      _logger.LogInformation("Handler {name}: Setting relay values to:\r\n{statuses}", this.Name, relaysAsString);

      foreach (var relay in relayValues)
      {
        if (relay.Value != null)
        {
          this.SetRelayState(relay.Key, relay.Value.Value);
        }
      }
    }

    public override void Dispose()
    {
      if (_port != null)
      {
        if (_port.IsOpen)
        {
          // if we are shutting down then lets set all the ports to off so that the dragonfly assumes the worst.
          for (int relayNumber = 1; relayNumber <= _options.Relays.Length; relayNumber++)
          {
            this.SetRelayState(relayNumber, false);
          }

          _port.Close();
        }

        _port.Dispose();
      }
    }

    private void SetRelayState(int channel, bool state)
    {
      if (_port == null)
        return;

      if (!_port.IsOpen)
        return;

      try
      {
        PortHelper.SendRelayCommand(_options.CommandType, _port, channel, state);
        _logger.LogInformation("Successfully sent command to relay: {channel}, state: {state}", channel, state);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to send command to relay: {channel}, state: {state}", channel, state);
      }
    }
  }
}
