using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using ObservatorySafety.Watchdog.Services;
using ObservatorySafety.Watchdog.Alerts;
using ObservatorySafety.Watchdog.Infrastructure;

namespace ObservatorySafety.Watchdog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(exeDir, "watchdog.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting ObservatorySafety.Watchdog...");

                var host = Host.CreateDefaultBuilder(args)
                    .UseWindowsService()
                    .UseSerilog()
                    .ConfigureAppConfiguration((hostingContext, config) =>
                    {
                        config.SetBasePath(exeDir);
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        var configuration = context.Configuration;

                        services.AddSingleton<LogTailer>();

                        services.AddSingleton<IAlertService, CompositeAlertService>();

                        services.AddSingleton<PushoverAlertService>();
                        services.AddSingleton<EmailAlertService>();
                        services.AddSingleton<WhatsAppAlertService>();

                        services.AddHostedService<WatchdogService>();
                    })
                    .Build();

                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "ObservatorySafety.Watchdog terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
