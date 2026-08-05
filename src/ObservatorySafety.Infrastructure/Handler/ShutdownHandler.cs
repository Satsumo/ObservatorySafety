using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure.ASCOM;
using ObservatorySafety.Infrastructure.Options;

namespace ObservatorySafety.Infrastructure.Handler
{
  public class ShutdownHandler : StatusHandlerBase
  {
    private readonly ILogger<ShutdownHandler> _logger;
    private readonly ShutdownOptions _options;

    private readonly ILogger<AscomClient> _ascomLogger;

    private bool _shutdownParametersMet = false;

    // this CTS controls the delayed shutdown task
    private CancellationTokenSource? _shutdownCts;

    public ShutdownHandler(ILogger<ShutdownHandler> logger, IOptions<ShutdownOptions> options, ILogger<AscomClient> ascomLogger)
    {
      _logger = logger;
      _options = options.Value;
      _ascomLogger = ascomLogger;

      Config = new StatusHandlerConfig()
      {
        NotificationType = StatusNotificationType.OnChange
      };
    }

    public override string Name => "Shutdown";

    public override StatusHandlerConfig Config { get; }

    public override void HandleMonitorStates(StatusPacket packet)
    {
      var shutdownRequired = false;

      var currentState = new Dictionary<StatusType, bool>();
      foreach (StatusType statusType in _options.ShutdownTriggerStatuses)
      {
        currentState.Add(statusType, true);

        foreach (var monitorState in packet.MonitorStates)
        {
          if (monitorState.Value.Statuses.ContainsKey(statusType))
          {
            var statusValue = monitorState.Value.Statuses[statusType];
            currentState[statusType] = statusValue;

            if (!statusValue)
            {
              shutdownRequired = true;
            }
            break;
          }
        }
      }

      if (shutdownRequired)
      {
        var statusesAsString = string.Join("\r\n", currentState.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        _logger.LogWarning("Shutdown required based on:\n\r{currentState}", currentState);
        HandleShutdown();
      }
      else
      {
        // If shutdown parameters are no longer met, cancel the timer
        _shutdownParametersMet = false;
        _shutdownCts?.Cancel();
      }
    }

    public override void Dispose()
    {
      _shutdownCts?.Cancel();
      _shutdownCts?.Dispose();
    }

    private async void HandleShutdown()
    {
      if (!_shutdownParametersMet)
      {
        _shutdownParametersMet = true;

        // Cancel any previous shutdown timer
        _shutdownCts?.Cancel();
        _shutdownCts?.Dispose();

        // Create a new CTS for this shutdown cycle
        _shutdownCts = new CancellationTokenSource();

        // Start the delayed shutdown
        _ = DelayedShutdownAsync(TimeSpan.FromMinutes(_options.ShutdownThresholdMinutes),
                                 () => _shutdownParametersMet,
                                 _shutdownCts.Token);
      }
    }

    private async Task DelayedShutdownAsync(
        TimeSpan duration,
        Func<bool> shutdownParametersMet,
        CancellationToken externalToken)
    {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
      var token = cts.Token;

      // Monitor shutdown condition in the background
      var monitorTask = Task.Run(async () =>
      {
        while (!token.IsCancellationRequested)
        {
          if (!shutdownParametersMet())
          {
            cts.Cancel();   // Trigger early exit
            return;
          }

          await Task.Delay(250, token); // Poll interval
        }
      }, token);

      try
      {
        // Wait for the duration unless cancelled
        await Task.Delay(duration, token);

        // Normal completion → do the final action
        ShutdownObservatory();
      }
      catch (OperationCanceledException)
      {
        // Early exit → do nothing
      }
    }

    private void ShutdownObservatory()
    {
      if (!_options.Enabled)
      {
        _logger.LogWarning("Shutdown threshold reached but shutdown is disabled in configuration, hence do nothing.");
        return;
      }

      _logger.LogWarning("Shutdown threshold reached — closing observatory.");
      foreach (var ascomHandler in _options.AscomHandlers)
      {
        using (var ascomClient = new AscomClient(_ascomLogger, ascomHandler.AscomID))
        {
          foreach (string switchName in ascomHandler.SwitchNames)
          {
            // find the switch ID to set based on the name in the configuration
            short targetSwitchID = 0;
            for (short switchID = 1; switchID < ascomClient.MaxSwitch; switchID++)
            {
              if (ascomClient.GetSwitchName(switchID).Equals(switchName))
              {
                targetSwitchID = switchID;
                break;
              }
            }

            if (targetSwitchID != 0)
            {
              _logger.LogWarning("Shutdown: Setting switch {switchID} on ascom device {ascomID} to true.", targetSwitchID, ascomHandler.AscomID);
              ascomClient.SetSwitchValue(targetSwitchID, true);

              Thread.Sleep(_options.DelayBetweenSwitchCallsSeconds * 1000);
            }
            else
            {
              // We did not find the configured switch name - so lets dump out a warning with the complete set of switch names
              // so that we can fix the config
              var switchNames = new List<string>();
              for (short switchID = 1; switchID < ascomClient.MaxSwitch; switchID++)
              {
                switchNames.Add(ascomClient.GetSwitchName(switchID));
              }
              _logger.LogWarning("Failed to find switch '{switchName}' on ascom device '{ascomID}'. Known switch nanes: {switchNames}",
                switchName, ascomHandler.AscomID, string.Join(",", switchNames));
            }
          }
        }
      }
      _shutdownParametersMet = false;
    }
  }
}
