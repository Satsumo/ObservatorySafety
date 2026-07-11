using System.Reflection;

using ObservatorySafety.Core;
using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;
using ObservatorySafety.Watchdog.Services;

using Serilog;
using Serilog.Settings.Configuration;

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

      try
      {
        Log.Information("Starting ObservatorySafety.Watchdog...");

        var builder = Host.CreateDefaultBuilder(args)
                          .UseConsoleLifetime();

        // CRITICAL: Apply Windows Service hosting BEFORE configuring services
        if (!runAsConsole)
        {
          Console.WriteLine("Configuring Windows Service hosting…");
          builder = builder.UseWindowsService();
        }

        builder
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
              Console.WriteLine("Configuring app configuration…");
              cfg.SetBasePath(exeDir);
              
              cfg.AddJsonFile("appsettings.json", optional: false);
              cfg.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json",
                              optional: true,
                              reloadOnChange: true);
            })
            .UseSerilog((ctx, services, loggerConfig) =>
            {
              Console.WriteLine("Configuring Serilog…");

              var options = new ConfigurationReaderOptions(
                      typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                      typeof(FileLoggerConfigurationExtensions).Assembly
                  );
                                
              // Read config first (console + file sink)
              loggerConfig
                  .ReadFrom.Configuration(ctx.Configuration, options)
                  .ReadFrom.Services(services);

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
                var logger = sp.GetRequiredService < ILogger <CompositeAlertService>>();
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
