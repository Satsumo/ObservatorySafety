using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using ObservatorySafety.Core.Abstractions;
using ObservatorySafety.Core.Status;

namespace ObservatorySafety.Core.Routing
{
  public sealed class StatusRouter
  {
    private readonly ILogger<StatusRouter> _logger;

    private readonly List<IStatusMonitor> _monitors = new();
    private readonly List<IStatusHandler> _handlers = new();
    private readonly ConcurrentDictionary<MonitorType, MonitorState> _latestStates = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastPollSent = new();

    public StatusRouter(ILogger<StatusRouter> logger)
    {
      _logger = logger;
    }

    public void RegisterMonitor(IStatusMonitor monitor)
    {
      _monitors.Add(monitor);
      monitor.StatusChanged += OnStatusChanged;
    }

    public void RegisterHandler(IStatusHandler handler)
    {
      _handlers.Add(handler);
    }

    public void StartAllMonitors()
    {
      foreach (var monitor in _monitors)
        monitor.Start();
    }

    public void StopAllMonitors()
    {
      foreach (var monitor in _monitors)
        monitor.Stop();
    }

    
    public void Poll()
    {
      var packet = BuildStatusPacket();
      var now = DateTime.UtcNow;

      foreach (var handler in _handlers)
      {
        if (handler.Config.NotificationType != StatusNotificationType.Poll)
          continue;

        var interval = handler.Config.PollingInterval ?? TimeSpan.Zero;
        if (interval <= TimeSpan.Zero)
          continue;

        var lastSent = _lastPollSent.GetOrAdd(handler.Name, DateTime.MinValue);
        if (now - lastSent >= interval)
        {
          handler.HandleMonitorStates(packet);
          _lastPollSent[handler.Name] = now;
        }
      }
    }

    private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
      _latestStates[e.State.MonitorType] = e.State;

      var packet = BuildStatusPacket();

      foreach (var handler in _handlers)
      {
        if (handler.Config.NotificationType == StatusNotificationType.OnChange)
        {
          handler.HandleMonitorStates(packet);
        }
      }
    }

    private StatusPacket BuildStatusPacket()
    {
      return new StatusPacket
      {
        TimestampUtc = DateTime.UtcNow,
        MonitorStates = new Dictionary<MonitorType, MonitorState>(_latestStates)
      };
    }
  }
}
