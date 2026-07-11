using System.Reflection;

using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Infrastructure.Simulation;
using ObservatorySafety.Service;

using Serilog;
using Serilog.Settings.Configuration;

static class Program
{
  private static String ARG_CONSOLE = "--console";
  private static String ARG_DRY_RUN = "--dry-run";
  private static String ARG_SIMULATE_POWER_LOSS = "--simulate-power-loss";

  public static async Task Main(string[] args)
  {
    Console.WriteLine("Program.Main starting…");

    bool runAsConsole = args.Contains(ARG_CONSOLE);
    bool dryRun = args.Contains(ARG_DRY_RUN);
    bool simulatePowerLoss = args.Contains(ARG_SIMULATE_POWER_LOSS);

    Console.WriteLine($"runAsConsole = {runAsConsole}");
    Console.WriteLine($"dryRun = {dryRun}");
    Console.WriteLine($"simulatePowerLoss = {simulatePowerLoss}");

    var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
    Console.WriteLine($"Executable directory: {exeDir}");

    var env = Environment.GetEnvironmentVariable("OBSERVATORY_ENVIRONMENT") ?? "Production";

    try
    {
      //
      // 1. Build configuration manually BEFORE host is built
      //
      var configuration = new ConfigurationBuilder()
          .SetBasePath(exeDir)
          .AddJsonFile("appsettings.json", optional: false)
          .AddJsonFile($"appsettings.{env}.json", optional: true)
          .Build();

      //
      // 2. Initialise Serilog BEFORE host is built
      //
      var options = new ConfigurationReaderOptions(
        typeof(ConsoleLoggerConfigurationExtensions).Assembly,
        typeof(FileLoggerConfigurationExtensions).Assembly
      );

      Log.Logger = new LoggerConfiguration()
          .ReadFrom.Configuration(configuration, options)
          .CreateLogger();

      Log.Information("Starting ObservatorySafety.Service...");

      //
      // 3. Build host
      //
      var builder = Host.CreateDefaultBuilder(args)
                        .UseConsoleLifetime()
                        .ConfigureLogging(logging =>
                        {
                          logging.ClearProviders();   // Ensure Serilog is the ONLY provider
                        })
                        .UseSerilog(Log.Logger)       // Use already-initialised Serilog
                        .ConfigureAppConfiguration((ctx, cfg) =>
                        {
                          Console.WriteLine("Configuring app configuration…");
                          cfg.SetBasePath(exeDir);

                          cfg.AddJsonFile("appsettings.json", optional: false);
                          cfg.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
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
                            var logger = s.GetRequiredService<ILogger<HttpService>>();
                            return new HttpService(logger, ninaOpts.BaseUrl, ninaOpts.ApiKey);
                          });

                          services.AddSingleton<IAstronomyApplicationClient>(sp =>
                          {
                            Console.WriteLine("Creating IAstronomyApplicationClient…");

                            if (dryRun)
                            {
                              Console.WriteLine("Using SimulatedClient (dry-run mode).");
                              var logger = sp.GetRequiredService<ILogger<SimulatedClient>>();
                              return new SimulatedClient(logger);
                            }
                            else
                            {
                              var httpService = sp.GetRequiredService<IHttpService>();
                              var equipmentOptions = sp.GetRequiredService<IOptions<EquipmentOptions>>().Value;
                              var logger = sp.GetRequiredService<ILogger<NinaScalarClient>>();

                              return new NinaScalarClient(logger, httpService, equipmentOptions);
                            }
                          });

                          services.AddSingleton<IPowerStatusProvider>(psp =>
                          {
                            Console.WriteLine("Creating IPowerStatusProvider…");

                            if (simulatePowerLoss)
                            {
                              Console.WriteLine("Using SimulatedPowerLossPowerStatusProvider.");
                              var logger = psp.GetRequiredService<ILogger<SimulatedPowerLossPowerStatusProvider>>();
                              return new SimulatedPowerLossPowerStatusProvider(logger);
                            }
                            else
                            {
                              var logger = psp.GetRequiredService<ILogger<WmiPowerStatusProvider>>();
                              return new WmiPowerStatusProvider(logger);
                            }
                          });

                          //
                          // Heartbeat hosted service
                          //
                          services.AddHostedService<SafetyHeartbeatService>();

                          //
                          // PowerMonitorService
                          //
                          services.AddSingleton<PowerMonitorService>(pms =>
                          {
                            Console.WriteLine("Creating PowerMonitorService…");

                            var powerStatusProvider = pms.GetRequiredService<IPowerStatusProvider>();
                            var safetyOpts = pms.GetRequiredService<IOptions<SafetyOptions>>().Value;
                            var logger = pms.GetRequiredService<ILogger<PowerMonitorService>>();

                            return new PowerMonitorService(
                                          logger,
                                          powerStatusProvider,
                                          TimeSpan.FromSeconds(safetyOpts.PowerOutageConfirmedThresholdSeconds)
                                      );
                          });

                          services.AddHostedService(sp =>
                          {
                            Console.WriteLine("Registering PowerMonitorService hosted service…");
                            return sp.GetRequiredService<PowerMonitorService>();
                          });

                          //
                          // SafetyService
                          //
                          services.AddHostedService(sp =>
                          {
                            Console.WriteLine("Registering SafetyService hosted service…");

                            var watcher = sp.GetRequiredService<PowerMonitorService>();
                            var orchestrator = sp.GetRequiredService<ShutdownOrchestrator>();
                            var nina = sp.GetRequiredService<IAstronomyApplicationClient>();
                            var logger = sp.GetRequiredService<ILogger<SafetyService>>();

                            return new SafetyService(logger, watcher, orchestrator, nina);
                          });
                        });

      Console.WriteLine("Building host…");
      var host = builder.Build();
      Console.WriteLine("Host built successfully.");

      Log.Information("ObservatorySafety.Service starting. Args:\n{Args}", String.Join("\n", args));

      Console.WriteLine("Starting host.RunAsync()…");

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
