using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;

namespace ObservatorySafety.Watchdog.Services
{
    public class WatchdogService : BackgroundService
    {
        private readonly ILogger<WatchdogService> _logger;
        private readonly IConfiguration _configuration;
        private readonly LogTailer _logTailer;
        private readonly IAlertService _alertService;

        private readonly string _serviceName;
        private readonly string _logPath;
        private readonly int _logInactivityThresholdSeconds;
        private readonly int _serviceCheckIntervalSeconds;
        private readonly string[] _alertStrings;

        private DateTime _lastLogActivity = DateTime.UtcNow;

        public WatchdogService(
            ILogger<WatchdogService> logger,
            IConfiguration configuration,
            LogTailer logTailer,
            IAlertService alertService)
        {
            _logger = logger;
            _configuration = configuration;
            _logTailer = logTailer;
            _alertService = alertService;

            var section = _configuration.GetSection("Watchdog");
            _serviceName = section.GetValue<string>("MainServiceName") ?? "ObservatorySafety.Service";
            _logPath = section.GetValue<string>("MainServiceLogPath") ?? "C:\\Observatory\\Logs\\observatory.log";
            _logInactivityThresholdSeconds = section.GetValue<int>("LogInactivityThresholdSeconds");
            _serviceCheckIntervalSeconds = section.GetValue<int>("ServiceCheckIntervalSeconds");
            _alertStrings = section.GetSection("AlertStrings").Get<string[]>() ?? Array.Empty<string>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WatchdogService started. Monitoring service {ServiceName} and log {LogPath}", _serviceName, _logPath);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckServiceHealthAsync(stoppingToken);
                    await CheckLogFileAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in WatchdogService loop");
                }

                await Task.Delay(TimeSpan.FromSeconds(_serviceCheckIntervalSeconds), stoppingToken);
            }

            _logger.LogInformation("WatchdogService stopping.");
        }

        private async Task CheckServiceHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == _serviceName);
                if (service == null)
                {
                    _logger.LogWarning("Main service {ServiceName} not found.", _serviceName);
                    await _alertService.SendAlertAsync(
                        "Observatory Service Missing",
                        $"Service '{_serviceName}' not found on system.",
                        cancellationToken);
                    return;
                }

                if (service.Status != ServiceControllerStatus.Running)
                {
                    _logger.LogWarning("Main service {ServiceName} is not running. Status: {Status}", _serviceName, service.Status);
                    await _alertService.SendAlertAsync(
                        "Observatory Service Not Running",
                        $"Service '{_serviceName}' status: {service.Status}",
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking service health");
            }
        }

        private async Task CheckLogFileAsync(CancellationToken cancellationToken)
        {
            var lines = await _logTailer.ReadNewLinesAsync(_logPath, cancellationToken);

            if (lines.Count > 0)
            {
                _lastLogActivity = DateTime.UtcNow;
            }

            foreach (var line in lines)
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
