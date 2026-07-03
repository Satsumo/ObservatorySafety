using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Service;

using Serilog;

using System;
using System.CommandLine;

var rootCommand = new RootCommand("Observatory Safety Controller");

// Flags
var consoleOption = new Option<bool>("--console", "Run as console instead of Windows Service");
var dryRunOption = new Option<bool>("--dry-run", "Do not call NINA API, only log actions");
var simulatePowerLossOption = new Option<bool>("--simulate-power-loss", "Trigger shutdown pipeline immediately");
var configOption = new Option<string?>("--config", "Path to custom appsettings.json");

rootCommand.AddOption(consoleOption);
rootCommand.AddOption(dryRunOption);
rootCommand.AddOption(simulatePowerLossOption);
rootCommand.AddOption(configOption);

rootCommand.SetHandler(async (bool console, bool dryRun, bool simulate, string? configPath) =>
{
  var builder = Host.CreateDefaultBuilder(args)
      .ConfigureAppConfiguration((ctx, cfg) =>
      {
        if (configPath != null)
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
          var log = sp.GetRequiredService<ILogger>();

          return new SafetyService(watcher, debouncer, orchestrator, nina, log, simulate);
        });
      });

  if (!console)
    builder.UseWindowsService();

  var host = builder.Build();

  Console.CancelKeyPress += (_, e) =>
  {
    e.Cancel = true;
    Console.WriteLine("Ctrl+C received, shutting down...");
    host.StopAsync().Wait();
  };

  await host.RunAsync();

}, consoleOption, dryRunOption, simulatePowerLossOption, configOption);

await rootCommand.InvokeAsync(args);
