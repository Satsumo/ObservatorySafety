using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Service;

using Serilog;
using Serilog.Events;

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
            cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
          }

        })
        .UseSerilog((ctx, services, loggerConfig) =>
        {
          loggerConfig
                  .ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services);

          // Look for debug logging level
          var arg = args.FirstOrDefault(a => a.StartsWith(ARG_LOGGING_LEVEL, StringComparison.OrdinalIgnoreCase));
          if (arg != null)
          {
            var levelText = arg.Split('=', 2)[1]; // get the value
            var level = Enum.Parse<LogEventLevel>(levelText, ignoreCase: true);
            loggerConfig.MinimumLevel.Is(level);
          }

        })
        .ConfigureServices((ctx, services) =>
        {
          services.Configure<NinaOptions>(ctx.Configuration.GetSection("Nina"));
          services.Configure<SafetyOptions>(ctx.Configuration.GetSection("Safety"));

          services.AddSingleton(sp =>
          {
            var safetyOpts = sp.GetRequiredService<IOptions<SafetyOptions>>().Value;
            return new StatusFileWatcher(safetyOpts);
          });

          services.AddSingleton(sp =>
          {
            var safetyOpts = sp.GetRequiredService<IOptions<SafetyOptions>>().Value;
            return new PowerLossDebouncer(TimeSpan.FromSeconds(safetyOpts.DebounceSeconds));
          });

          services.AddSingleton<ShutdownOrchestrator>();

          services.AddSingleton<INinaClient>(sp =>
          {
            var ninaOpts = sp.GetRequiredService<IOptions<NinaOptions>>().Value;
            return new NinaScalarClient(ninaOpts, dryRun);
          });

          services.AddHostedService(sp =>
          {
            var watcher = sp.GetRequiredService<StatusFileWatcher>();
            var debouncer = sp.GetRequiredService<PowerLossDebouncer>();
            var orchestrator = sp.GetRequiredService<ShutdownOrchestrator>();
            var nina = sp.GetRequiredService<INinaClient>();
            var log = sp.GetRequiredService<Serilog.ILogger>();

            return new SafetyService(watcher, debouncer, orchestrator, nina, log, simulatePowerLoss);
          });

          services.AddHostedService(sp =>
          {
            var opts = sp.GetRequiredService<IOptions<SafetyOptions>>().Value;
            var log = sp.GetRequiredService<Serilog.ILogger>();
            return new PowerMonitorService(opts.GetExpandedFlagFilePath(), log);
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
