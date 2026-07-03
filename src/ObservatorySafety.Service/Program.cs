using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;

using ObservatorySafety.Core;
using ObservatorySafety.Infrastructure;
using ObservatorySafety.Service;

using Serilog;

Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
      cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
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
        return new NinaScalarClient(ninaOpts);
      });

      services.AddHostedService<SafetyService>();
    })
    .Build()
    .Run();
