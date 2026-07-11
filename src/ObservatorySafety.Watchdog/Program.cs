using System.Reflection;

using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;
using ObservatorySafety.Watchdog.Services;

using Serilog;

namespace ObservatorySafety.Watchdog
{
  public class Program
  {
    private static String ARG_CONSOLE = "--console";

    public static async Task Main(string[] args)
    {
      Console.WriteLine("Program.Main starting…");

      bool runAsConsole = args.Contains(ARG_CONSOLE);

      Console.WriteLine($"runAsConsole = {runAsConsole}");

      var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      Console.WriteLine($"Executable directory: {exeDir}");

      var env = Environment.GetEnvironmentVariable("OBSERVATORY_ENVIRONMENT");
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
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
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
                          .ConfigureServices((context, services) =>
                          {
                            var configuration = context.Configuration;

                            services.AddSingleton<LogTailer>();

                            services.AddSingleton<PushoverAlertService>();
                            services.AddSingleton<EmailAlertService>();
                            services.AddSingleton<WhatsAppAlertService>();

                            services.AddSingleton<IAlertService>(sp =>
                            {
                              var config = sp.GetRequiredService<IConfiguration>();
                              var logger = sp.GetRequiredService<ILogger<CompositeAlertService>>();
                              var composite = new CompositeAlertService(logger, config);

                              composite.AddAlertService("Pushover", sp.GetRequiredService<PushoverAlertService>());
                              composite.AddAlertService("Email", sp.GetRequiredService<EmailAlertService>());
                              composite.AddAlertService("WhatsApp", sp.GetRequiredService<WhatsAppAlertService>());

                              return composite;
                            });

                            services.AddHostedService<WatchdogService>();
                          });

        Console.WriteLine("Building host…");
        var host = builder.Build();
        Console.WriteLine("Host built successfully.");

        // Guaranteed startup log (creates the log file)
        Log.Information("ObservatorySafety.Watchdog starting. Args:\n{Args}", String.Join("\n", args));
        await host.RunAsync();
      }
      catch (Exception ex)
      {
        Log.Fatal(ex, "Fatal startup exception in ObservatorySafety.Watchdog");
        Console.WriteLine($"Fatal startup exception: {ex}");
      }
      finally
      {
        Log.CloseAndFlush();
        Console.WriteLine("Host shutdown complete.");
      }
    }
  }
}
