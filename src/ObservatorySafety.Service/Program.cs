using System.Reflection;

using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Infrastructure.Simulation;
using ObservatorySafety.Service;

using Serilog;
using Serilog.Events;
using Serilog.Settings.Configuration;

static class Program
{
  private static String ARG_CONSOLE = "--console";
  private static String ARG_DRY_RUN = "--dry-run";
  private static String ARG_SIMULATE_POWER_LOSS = "--simulate-power-loss";
  private static String ARG_LOGGING_LEVEL = "--logging-minimumLevel";
  private static String ARG_CONFIG = "--config";

  public static async Task Main(string[] args)
  {
    bool runAsConsole = args.Contains(ARG_CONSOLE);
    bool dryRun = args.Contains(ARG_DRY_RUN);
    bool simulatePowerLoss = args.Contains(ARG_SIMULATE_POWER_LOSS);

    string? configPath = null;
    var configIndex = Array.IndexOf(args, ARG_CONFIG);
    if (configIndex >= 0 && configIndex + 1 < args.Length)
    {
      configPath = args[configIndex + 1];
    }

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((ctx, cfg) =>
        {
          if (!string.IsNullOrWhiteSpace(configPath))
          {
            cfg.AddJsonFile(configPath, optional: false, reloadOnChange: true);
          }
          else
          {
            cfg.AddJsonFile("appsettings.json", optional: false);
          }

        })
        .UseSerilog((ctx, services, loggerConfig) =>
        {
          var options = new ConfigurationReaderOptions(
              typeof(ConsoleLoggerConfigurationExtensions).Assembly,
              typeof(FileLoggerConfigurationExtensions).Assembly
          );

          loggerConfig
              .ReadFrom.Configuration(ctx.Configuration, options)
              .ReadFrom.Services(services);

          // Your logging-minimumLevel override stays the same
          var arg = args.FirstOrDefault(a => a.StartsWith(ARG_LOGGING_LEVEL, StringComparison.OrdinalIgnoreCase));
          if (arg != null)
          {
            var levelText = arg.Split('=', 2)[1];
            var level = Enum.Parse<LogEventLevel>(levelText, ignoreCase: true);
            loggerConfig.MinimumLevel.Is(level);
          }
        })
        .ConfigureServices((ctx, services) =>
        {
          services.Configure<NinaOptions>(ctx.Configuration.GetSection("Nina"));
          services.Configure<SafetyOptions>(ctx.Configuration.GetSection("Safety"));
          services.Configure<EquipmentOptions>(ctx.Configuration.GetSection("Equipment"));

          services.AddSingleton<ShutdownOrchestrator>();

          services.AddSingleton<IHttpService>(s =>
          {
            var ninaOpts = s.GetRequiredService<IOptions<NinaOptions>>().Value;
            var baseUrl = ninaOpts.BaseUrl;
            var apiKey = ninaOpts.ApiKey;

            return new HttpService(baseUrl, apiKey);
          });
          
          services.AddSingleton<IAstronomyApplicationClient>(sp =>
          {
            if (dryRun)
            {
              return new SimulatedClient();
            }
            var httpService = sp.GetRequiredService<IHttpService>();
            var equipmentOptions = sp.GetRequiredService<IOptions<EquipmentOptions>>().Value;

            return new NinaScalarClient(httpService, equipmentOptions);
          });

          services.AddSingleton<IPowerStatusProvider>(psp => {
            if (simulatePowerLoss)
            {
              return new SimulatedPowerLossPowerStatusProvider();
            }
            else
            {
              return new WmiPowerStatusProvider();
            }
          });

          services.AddSingleton<PowerMonitorService>(pms => {
            var powerStatusProvider = pms.GetRequiredService<IPowerStatusProvider>();
            var safetyOpts = pms.GetRequiredService<IOptions<SafetyOptions>>().Value;

            return new PowerMonitorService(powerStatusProvider, TimeSpan.FromSeconds(safetyOpts.PowerOutageConfirmedThresholdSeconds));
          });

          services.AddHostedService(sp => sp.GetRequiredService<PowerMonitorService>());

          services.AddHostedService(sp =>
          {
            var watcher = sp.GetRequiredService<PowerMonitorService>();
            var orchestrator = sp.GetRequiredService<ShutdownOrchestrator>();
            var nina = sp.GetRequiredService<IAstronomyApplicationClient>();

            return new SafetyService(watcher, orchestrator, nina);
          });

          

        });

    if (!runAsConsole)
    {
      builder.UseWindowsService();
    }

    var host = builder.Build();

    Console.CancelKeyPress += (_, e) =>
    {
      e.Cancel = true;
      Console.WriteLine("Ctrl+C received, shutting down...");
      host.StopAsync().Wait();
    };

    // Capture the logger factory here — AFTER the host is built
    LogProvider.Factory = host.Services.GetRequiredService<ILoggerFactory>();

    // Log the startup message with arguments
    LogProvider.Factory.CreateLogger<SafetyService>().Log(LogLevel.Information,
      $"Starting ObservatorySafety.Service:\nStarting with args:\n{String.Join("\n", args)}\n");

    await host.RunAsync();
  }
}
