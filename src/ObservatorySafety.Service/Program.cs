using Microsoft.Extensions.Options;

using ObservatorSafety.NINA;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Abstractions;

using ObservatorySafety.Core.Routing;
using ObservatorySafety.Core.Status;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Infrastructure.Alerts;
using ObservatorySafety.Infrastructure.ASCOM;
using ObservatorySafety.Infrastructure.Configuration;
using ObservatorySafety.Infrastructure.Handler;
using ObservatorySafety.Infrastructure.Monitor;
using ObservatorySafety.Infrastructure.Options;
using ObservatorySafety.Infrastructure.Simulation;
using ObservatorySafety.NINA;

using Serilog;
using Serilog.Settings.Configuration;

using System.Reflection;

static class Program
{
  private static String ARG_CONSOLE = "--console";
  private static String ARG_DRY_RUN = "--dry-run";
  private static String ARG_SIMULATE_POWER_LOSS = "--simulate-power-loss";

  public static async Task Main(string[] args)
  {
    ConsoleLog("Program.Main starting…");

    bool runAsConsole = args.Contains(ARG_CONSOLE);
    bool dryRun = args.Contains(ARG_DRY_RUN);
    bool simulatePowerLoss = args.Contains(ARG_SIMULATE_POWER_LOSS);

    ConsoleLog($"runAsConsole = {runAsConsole}");
    ConsoleLog($"dryRun = {dryRun}");
    ConsoleLog($"simulatePowerLoss = {simulatePowerLoss}");

    Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

    var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if (exeDir == null)
    {
      exeDir = AppContext.BaseDirectory;
    }
    ConsoleLog($"Executable directory: {exeDir}");

    var baseDir = Directory.GetCurrentDirectory();
    ConsoleLog($"Base directory: {baseDir}");

    var env = Environment.GetEnvironmentVariable("OBSERVATORY_ENVIRONMENT") ?? "Production";
    ConsoleLog($"Environment: {env}");

    try
    {
      //
      // 1. Build configuration manually BEFORE host is built
      //
      var configuration = new ConfigurationBuilder()
          .SetBasePath(baseDir)
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
          .ConfigureLogging(logging =>
          {
            logging.ClearProviders();   // Ensure Serilog is the ONLY provider
          })
          .UseSerilog(Log.Logger)
          .ConfigureAppConfiguration((ctx, cfg) =>
          {
            ConsoleLog("Configuring app configuration…");
            cfg.SetBasePath(baseDir);

            cfg.AddJsonFile("appsettings.json", optional: false);
            cfg.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
          })
          .ConfigureServices((ctx, services) =>
          {
            ConsoleLog("Configuring services…");

            //
            // Options
            //
            services.Configure<EquipmentOptions>(ctx.Configuration.GetSection("Equipment"));

            // Monitor options
            services.Configure<CloudWatcherOptions>(ctx.Configuration.GetSection("CloudWatcher"));
            services.Configure<DarkDragonOptions>(ctx.Configuration.GetSection("DarkDragon"));
            services.Configure<DragonflyOptions>(ctx.Configuration.GetSection("Dragonfly"));
            services.Configure<NinaOptions>(ctx.Configuration.GetSection("Nina"));
            services.Configure<PowerOptions>(ctx.Configuration.GetSection("Power"));

            // Handler options
            services.Configure<USBRelaySwitchHandlerOptions>(ctx.Configuration.GetSection("USBRelaySwitch"));
            services.Configure<ShutdownOptions>(ctx.Configuration.GetSection("Shutdown"));


            // Alert services' options
            services.Configure<EmailAlertOptions>(ctx.Configuration.GetSection("Email"));
            services.Configure<WhatsAppAlertOptions>(ctx.Configuration.GetSection("WhatsApp"));
            services.Configure<PushOverAlertOptions>(ctx.Configuration.GetSection("PushOver"));

            //
            // Astronomy client (NINA or simulated)
            //
            services.AddSingleton<INinaClient>(sp =>
            {
              ConsoleLog("Creating INinaClient…");

              if (dryRun)
              {
                ConsoleLog("Using SimulatedClient (dry-run mode).");
                var logger = sp.GetRequiredService<ILogger<SimulatedClient>>();
                return new SimulatedClient(logger);
              }
              else
              {
                ConsoleLog("Creating NINA HttpService…");
                var ninaOpts = sp.GetRequiredService<IOptions<NinaOptions>>().Value;
                var httpService = new HttpService(sp.GetRequiredService<ILogger<HttpService>>(), ninaOpts.BaseUrl, ninaOpts.ApiKey);

                var equipmentOptions = sp.GetRequiredService<IOptions<EquipmentOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<NinaClient>>();

                return new NinaClient(logger, httpService, equipmentOptions);
              }
            });

            // Alert servcices
            services.AddSingleton<PushoverAlertService>();
            services.AddSingleton<EmailAlertService>();
            services.AddSingleton<WhatsAppAlertService>();
            services.AddSingleton<IAlertService>(sp =>
            {
              var logger = sp.GetRequiredService<ILogger<CompositeAlertService>>();
              var composite = new CompositeAlertService(logger);

              composite.AddAlertService("Pushover", sp.GetRequiredService<PushoverAlertService>());
              composite.AddAlertService("Email", sp.GetRequiredService<EmailAlertService>());
              composite.AddAlertService("WhatsApp", sp.GetRequiredService<WhatsAppAlertService>());

              return composite;
            });

            //
            // Monitors
            //
            if (simulatePowerLoss)
            {
              ConsoleLog("Using SimulatedPowerLossPowerStatusMonitor.");
              services.AddSingleton<IStatusMonitor, SimulatedPowerLossPowerStatusMonitor>();
            }
            else
            {
              services.AddSingleton<IStatusMonitor, WmiPowerStatusMonitor>();
            }
            services.AddSingleton<IStatusMonitor, NinaMonitor>();
            services.AddSingleton<IStatusMonitor, CloudWatcherMonitor>();
            services.AddSingleton<IStatusMonitor, HeartbeatMonitor>();
            services.AddSingleton<IStatusMonitor, DragomflyMonitor>(sp =>
            {
              var options = sp.GetRequiredService<IOptions<DragonflyOptions>>();
              var ascomClient = new AscomClient(sp.GetRequiredService<ILogger<AscomClient>>(), options.Value.AscomID);

              var logger = sp.GetRequiredService<ILogger<DragomflyMonitor>>();
              return new DragomflyMonitor(logger, options, ascomClient);
            });

            services.AddSingleton<IStatusMonitor>(s =>
            {
              ConsoleLog("Creating NINA HttpService…");
              var darkDragonOpts = s.GetRequiredService<IOptions<DarkDragonOptions>>();
              var httpService = new HttpService(s.GetRequiredService<ILogger<HttpService>>(), darkDragonOpts.Value.BaseUrl, darkDragonOpts.Value.ApiKey);

              var logger = s.GetRequiredService<ILogger<DarkDragonMountParkedMonitor>>();
              return new DarkDragonMountParkedMonitor(logger, darkDragonOpts, httpService);
            });

            //
            // Handlers
            //
            services.AddSingleton<IStatusHandler, USBRelaySwitchHandler>();
            services.AddSingleton<IStatusHandler, NotificationHandler>();
            services.AddSingleton<IStatusHandler, ShutdownHandler>();

            //
            // StatusRouter
            //
            services.AddSingleton<StatusRouter>(sp =>
            {
              var logger = sp.GetRequiredService<ILogger<StatusRouter>>();
              var router = new StatusRouter(logger);

              var monitors = sp.GetServices<IStatusMonitor>();
              var handlers = sp.GetServices<IStatusHandler>();

              foreach ( var monitor in monitors )
              {
                router.RegisterMonitor(monitor);
              }

              foreach ( var handler in handlers )
              {
                router.RegisterHandler(handler);
              }
              return router;
            });

            //
            // StatusRouterWorker - runs the status router
            //
            services.AddHostedService<StatusRouterWorker>();
          });

      if (!runAsConsole)
      {
        builder.UseWindowsService();
      }

      ConsoleLog("Building host…");
      var host = builder.Build();
      ConsoleLog("Host built successfully.");

      Log.Information("ObservatorySafety.Service starting. Args:\n{Args}", String.Join("\n", args));

      ConsoleLog("Starting host.RunAsync()…");

      await host.RunAsync();
    }
    catch (Exception ex)
    {
      Log.Fatal(ex, "Fatal startup exception in ObservatorySafety.Service: {Message}", ex.Message);
      ConsoleLog($"Fatal startup exception: {ex}");
    }
    finally
    {
      Log.CloseAndFlush();
      ConsoleLog("Host shutdown complete.");
    }

    static void ConsoleLog(string message)
    {
      if (Environment.GetCommandLineArgs().Contains("--console"))
        Console.WriteLine(message);
    }
  }
}
