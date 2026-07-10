using System.Diagnostics;
using System.ServiceProcess;

using ObservatorySafety.Core;
using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;

namespace ObservatorySafety.Watchdog.Services
{
  public class WatchdogService : BackgroundService
  {
    private const int MaxBackoffSeconds = 300; // 5 minutes

    private ILogger<WatchdogService>? _loggerBase;
    private ILogger<WatchdogService> _logger =>
        _loggerBase ??= LogProvider.Factory!.CreateLogger<WatchdogService>();

    private readonly IConfiguration _configuration;
    private readonly LogTailer _logTailer;
    private readonly IAlertService _alertService;

    private readonly string _serviceName;
    private readonly string _logDirectory;
    private readonly string _logPattern;
    private readonly int _logInactivityThresholdSeconds;

    private readonly int _serviceCheckIntervalSeconds;
    private readonly int _logCheckIntervalSeconds;

    private readonly string[] _alertStrings;

    private DateTime _lastLogActivity = DateTime.UtcNow;

    private long _lastLogPosition = 0;
    private string? _lastLogFile = null;

    // Backoff state
    private int _serviceErrorBackoffSeconds = 0;
    private int _logErrorBackoffSeconds = 0;

    public WatchdogService(
        IConfiguration configuration,
        LogTailer logTailer,
        IAlertService alertService)
    {
      _configuration = configuration;
      _logTailer = logTailer;
      _alertService = alertService;

      var section = _configuration.GetSection("Watchdog");

      _serviceName = section.GetValue<string>("MainServiceName") ?? "ObservatorySafety.Service";
      _logDirectory = section.GetValue<string>("MainServiceLogDirectory")!;
      _logPattern = section.GetValue<string>("MainServiceLogPattern")!;
      _logInactivityThresholdSeconds = section.GetValue<int>("LogInactivityThresholdSeconds");

      _serviceCheckIntervalSeconds = section.GetValue<int>("ServiceCheckIntervalSeconds");
      _logCheckIntervalSeconds = section.GetValue<int>("LogCheckIntervalSeconds");

      _alertStrings = section.GetSection("AlertStrings").Get<string[]>() ?? Array.Empty<string>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      _logger.LogInformation(
          "WatchdogService started. Monitoring service {ServiceName} and log {LogDirectory}\\{LogPattern}",
          _serviceName, _logDirectory, _logPattern);

      InitialiseTailPosition();

      var tasks = new List<Task>();

      if (_serviceCheckIntervalSeconds > 0)
      {
        _logger.LogInformation(
            "Service health checker enabled (interval = {Interval}s)",
            _serviceCheckIntervalSeconds);

        tasks.Add(RunServiceHealthLoop(stoppingToken));
      }
      else
      {
        _logger.LogInformation("Service health checker DISABLED (interval = 0)");
      }

      if (_logCheckIntervalSeconds > 0)
      {
        _logger.LogInformation(
            "Log tail checker enabled (interval = {Interval}s)",
            _logCheckIntervalSeconds);

        tasks.Add(RunLogTailLoop(stoppingToken));
      }
      else
      {
        _logger.LogInformation("Log tail checker DISABLED (interval = 0)");
      }

      if (tasks.Count == 0)
      {
        _logger.LogWarning("All watchdog checkers disabled. Service will idle.");
        return;
      }

      await Task.WhenAll(tasks);

      _logger.LogInformation("WatchdogService stopping.");
    }

    private void InitialiseTailPosition()
    {
      var latestLogFile = _logTailer.GetLatestLogFile(_logDirectory, _logPattern);

      if (latestLogFile == null)
      {
        _logger.LogWarning("No log file found at startup.");
        return;
      }

      try
      {
        var fileInfo = new FileInfo(latestLogFile);
        _lastLogFile = latestLogFile;
        _lastLogPosition = fileInfo.Length;

        _logger.LogInformation(
            "Startup tail initialised. File: {File}, Offset: {Offset}",
            latestLogFile, _lastLogPosition);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to initialise tail position.");
        _lastLogPosition = 0;
      }
    }

    // -------------------------------
    // SERVICE HEALTH LOOP (with backoff)
    // -------------------------------
    private async Task RunServiceHealthLoop(CancellationToken token)
    {
      while (!token.IsCancellationRequested)
      {
        try
        {
          await CheckServiceHealthAsync(token);

          // Reset backoff on success
          _serviceErrorBackoffSeconds = 0;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error in service health loop");

          // Increase backoff (exponential)
          if (_serviceErrorBackoffSeconds == 0)
            _serviceErrorBackoffSeconds = _serviceCheckIntervalSeconds;
          else
            _serviceErrorBackoffSeconds = Math.Min(
                _serviceErrorBackoffSeconds * 2,
                MaxBackoffSeconds);

          _logger.LogWarning(
              "Service health check failed. Backing off for {Seconds} seconds.",
              _serviceErrorBackoffSeconds);
        }

        var delaySeconds = (_serviceErrorBackoffSeconds > 0)
            ? _serviceErrorBackoffSeconds
            : _serviceCheckIntervalSeconds;

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
      }
    }

    // -------------------------------
    // LOG TAIL LOOP (with backoff)
    // -------------------------------
    private async Task RunLogTailLoop(CancellationToken token)
    {
      while (!token.IsCancellationRequested)
      {
        try
        {
          await CheckLogFileAsync(token);

          // Reset backoff on success
          _logErrorBackoffSeconds = 0;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error in log tail loop");

          // Increase backoff (exponential)
          if (_logErrorBackoffSeconds == 0)
            _logErrorBackoffSeconds = _logCheckIntervalSeconds;
          else
            _logErrorBackoffSeconds = Math.Min(
                _logErrorBackoffSeconds * 2,
                MaxBackoffSeconds);

          _logger.LogWarning(
              "Log tail check failed. Backing off for {Seconds} seconds.",
              _logErrorBackoffSeconds);
        }

        var delaySeconds = (_logErrorBackoffSeconds > 0)
            ? _logErrorBackoffSeconds
            : _logCheckIntervalSeconds;

        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
      }
    }

    private async Task CheckServiceHealthAsync(CancellationToken cancellationToken)
    {
      try
      {
        bool serviceRunning = false;

        // 1 - Check Windows Service
        var service = ServiceController.GetServices()
            .FirstOrDefault(s => s.ServiceName == _serviceName);

        if (service != null && service.Status == ServiceControllerStatus.Running)
        {
          serviceRunning = true;
        }

        // 2 - Check console process
        if (!serviceRunning)
        {
          var processes = Process.GetProcessesByName(_serviceName);

          if (processes.Length > 0)
          {
            _logger.LogInformation(
                "SafetyService running as console process: {ProcessName}",
                _serviceName);

            serviceRunning = true;
          }
          else
          {
            var processName = Path.GetFileNameWithoutExtension(_serviceName);

            processes = Process.GetProcessesByName(processName);

            if (processes.Length > 0)
            {
              _logger.LogInformation(
                  "SafetyService running as console process: {ProcessName}",
                  processName);

              serviceRunning = true;
            }
          }
        }

        // 3 - If neither running → alert
        if (!serviceRunning)
        {
          _logger.LogWarning(
              "SafetyService '{ServiceName}' is not running as Windows Service or console process.",
              _serviceName);

          await _alertService.SendAlertAsync(
              "Observatory SafetyService Not Running",
              $"SafetyService '{_serviceName}' is not running as Windows Service or console process.",
              cancellationToken);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking service health");
        throw;
      }
    }
    private async Task CheckLogFileAsync(CancellationToken cancellationToken)
    {
      var latestLogFile = _logTailer.GetLatestLogFile(_logDirectory, _logPattern);

      if (latestLogFile == null)
      {
        _logger.LogWarning("No log files found in {Directory} matching {Pattern}", _logDirectory, _logPattern);
        return;
      }

      if (_lastLogFile != latestLogFile)
      {
        _logger.LogInformation("New log file detected. Resetting tail position. File: {File}", latestLogFile);
        _lastLogFile = latestLogFile;
        _lastLogPosition = 0;
      }

      var (newLines, newPosition) = await _logTailer.ReadNewLinesFromOffsetAsync(
          latestLogFile,
          _lastLogPosition,
          cancellationToken);

      _lastLogPosition = newPosition;

      if (newLines.Count > 0)
      {
        _lastLogActivity = DateTime.UtcNow;
      }

      foreach (var line in newLines)
      {
        foreach (var pattern in _alertStrings)
        {
          if (!string.IsNullOrWhiteSpace(pattern) &&
              line.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
          {
            _logger.LogWarning("Alert pattern '{Pattern}' detected in log line: {Line}", pattern, line);

            await _alertService.SendAlertAsync(
                "Observatory Log Alert",
                $"Pattern '{pattern}' detected in log: {line}",
                cancellationToken);
          }
        }
      }

      var inactivitySeconds = (DateTime.UtcNow - _lastLogActivity).TotalSeconds;

      if (inactivitySeconds > _logInactivityThresholdSeconds)
      {
        _logger.LogWarning("Log inactivity detected. No log updates for {Seconds} seconds.", inactivitySeconds);

        await _alertService.SendAlertAsync(
            "Observatory Log Inactivity",
            $"No log updates for {inactivitySeconds:F0} seconds. Service may be hung.",
            cancellationToken);

        _lastLogActivity = DateTime.UtcNow;
      }
    }
  }
}
