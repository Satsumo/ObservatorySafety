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
    Console.WriteLine("Program.Main starting…");

    bool runAsConsole = args.Contains(ARG_CONSOLE);
    bool dryRun = args.Contains(ARG_DRY_RUN);
    bool simulatePowerLoss = args.Contains(ARG_SIMULATE_POWER_LOSS);

    Console.WriteLine($"runAsConsole = {runAsConsole}");
    Console.WriteLine($"dryRun = {dryRun}");
    Console.WriteLine($"simulatePowerLoss = {simulatePowerLoss}");

    string? configPath = null;
    var configIndex = Array.IndexOf(args, ARG_CONFIG);
    if (configIndex >= 0 && configIndex + 1 < args.Length)
    {
      configPath = args[configIndex + 1];
      Console.WriteLine($"Using custom config path: {configPath}");
    }
    else
    {
      Console.WriteLine("Using default config path: appsettings.json");
    }

    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((ctx, cfg) =>
        {
          Console.WriteLine("Configuring app configuration…");

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
          Console.WriteLine("Configuring Serilog…");

          var options = new ConfigurationReaderOptions(
                  typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                  typeof(FileLoggerConfigurationExtensions).Assembly
              );

          loggerConfig
                  .ReadFrom.Configuration(ctx.Configuration, options)
                  .ReadFrom.Services(services);

          var arg = args.FirstOrDefault(a => a.StartsWith(ARG_LOGGING_LEVEL, StringComparison.OrdinalIgnoreCase));
          if (arg != null)
          {
            var levelText = arg.Split('=', 2)[1];
            var level = Enum.Parse<LogEventLevel>(levelText, ignoreCase: true);
            loggerConfig.MinimumLevel.Is(level);

            Console.WriteLine($"Minimum logging level overridden to: {level}");
          }
        })
        .ConfigureServices((ctx, services) =>
        {
          Console.WriteLine("Configuring services…");

          services.Configure<NinaOptions>(ctx.Configuration.GetSection("Nina"));
          services.Configure<SafetyOptions>(ctx.Configuration.GetSection("Safety"));
          services.Configure<EquipmentOptions>(ctx.Configuration.GetSection("Equipment"));

          services.AddSingleton<ShutdownOrchestrator>();

          services.AddSingleton<IHttpService>(s =>
          {
            Console.WriteLine("Creating IHttpService…");

            var ninaOpts = s.GetRequiredService<IOptions<NinaOptions>>().Value;
            return new HttpService(ninaOpts.BaseUrl, ninaOpts.ApiKey);
          });

          services.AddSingleton<IAstronomyApplicationClient>(sp =>
          {
            Console.WriteLine("Creating IAstronomyApplicationClient…");

            if (dryRun)
            {
              Console.WriteLine("Using SimulatedClient (dry-run mode).");
              return new SimulatedClient();
            }

            var httpService = sp.GetRequiredService<IHttpService>();
            var equipmentOptions = sp.GetRequiredService<IOptions<EquipmentOptions>>().Value;

            return new NinaScalarClient(httpService, equipmentOptions);
          });

          services.AddSingleton<IPowerStatusProvider>(psp =>
          {
            Console.WriteLine("Creating IPowerStatusProvider…");

            if (simulatePowerLoss)
            {
              Console.WriteLine("Using SimulatedPowerLossPowerStatusProvider.");
              return new SimulatedPowerLossPowerStatusProvider();
            }

            return new WmiPowerStatusProvider();
          });

          services.AddSingleton<PowerMonitorService>(pms =>
          {
            Console.WriteLine("Creating PowerMonitorService…");

            var powerStatusProvider = pms.GetRequiredService<IPowerStatusProvider>();
            var safetyOpts = pms.GetRequiredService<IOptions<SafetyOptions>>().Value;

            return new PowerMonitorService(
                    powerStatusProvider,
                    TimeSpan.FromSeconds(safetyOpts.PowerOutageConfirmedThresholdSeconds)
                );
          });

          services.AddHostedService(sp =>
          {
            Console.WriteLine("Registering PowerMonitorService hosted service…");
            return sp.GetRequiredService<PowerMonitorService>();
          });

          services.AddHostedService(sp =>
          {
            Console.WriteLine("Registering SafetyService hosted service…");

            var watcher = sp.GetRequiredService<PowerMonitorService>();
            var orchestrator = sp.GetRequiredService<ShutdownOrchestrator>();
            var nina = sp.GetRequiredService<IAstronomyApplicationClient>();

            return new SafetyService(watcher, orchestrator, nina);
          });
        });

    // CRITICAL: Apply Windows Service hosting BEFORE Build()
    if (!runAsConsole)
    {
      Console.WriteLine("Configuring Windows Service hosting…");
      builder = builder.UseWindowsService();
    }

    Console.WriteLine("Building host…");
    var host = builder.Build();
    Log.Information("Host built successfully.");

    // CRITICAL: Set LogProvider.Factory BEFORE hosted services start
    LogProvider.Factory = host.Services.GetRequiredService<ILoggerFactory>();
    Log.Information("LoggerFactory assigned to LogProvider.Factory.");


    Console.CancelKeyPress += (_, e) =>
    {
      e.Cancel = true;
      Console.WriteLine("Ctrl+C received, shutting down…");
      host.StopAsync().Wait();
    };

    // Capture the logger factory AFTER the host is built
    Log.Information($"Starting ObservatorySafety.Service with args:\n{String.Join("\n", args)}");

    Console.WriteLine("Starting host.RunAsync()…");

    try
    {
      await host.RunAsync();
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "Fatal startup exception in ObservatorySafety.Service");
      Console.WriteLine($"Fatal startup exception: {ex}");
    }
    finally
    {
      Log.CloseAndFlush();
      Console.WriteLine("Host shutdown complete.");
    }
  }
}
