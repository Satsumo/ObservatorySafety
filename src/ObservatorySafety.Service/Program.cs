using Microsoft.Extensions.Options;
using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Service;
using Serilog;

static class Program
{
  public static async Task Main(string[] args)
  {
    bool runAsConsole = args.Contains("--console");
    bool dryRun = args.Contains("--dry-run");
    bool simulatePowerLoss = args.Contains("--simulate-power-loss");

    string? configPath = null;
    var configIndex = Array.IndexOf(args, "--config");
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

    await host.RunAsync();
  }
}
