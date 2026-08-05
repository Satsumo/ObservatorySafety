using Microsoft.Extensions.Logging;

using ObservatorySafety.Infrastructure.ASCOM;

using System.CommandLine;

namespace ObservatoryUtility
{
  public class Program
  {
    // Supported calls:
    // ObservatoryService.Utility.exe ascom --name "ASCOM.SkyWatcher.Telescope"
    static int Main(string[] args)
    {

      var loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddSimpleConsole(options =>
        {
          options.SingleLine = true;
          options.TimestampFormat = "HH:mm:ss ";
        });
      });


      // Root command
      var root = new RootCommand("ObservatorySafety Utility");

      // ascom command
      var ascomCommand = new Command("ascom", "Inspect an ASCOM device");

      // --name option
      var nameOption = new Option<string>("--name")
      {
        Description = "The ASCOM device name"
      };

      // Add option to the command
      ascomCommand.Options.Add(nameOption);

      // Add command to root
      root.Subcommands.Add(ascomCommand);

      // Parse args
      var parseResult = root.Parse(args);

      // Check if "ascom" was invoked
      if (parseResult.CommandResult.Command == ascomCommand)
      {
        // Retrieve option value using GetValue (correct for 2.0.10)
        var deviceName = parseResult.GetValue(nameOption);
        if (deviceName != null)
        {
          InspectAscomDevice(loggerFactory, deviceName);
        }
      }
      else
      {
        Console.WriteLine("No valid command provided. Use --help for usage information.");
      }
      return 0;
    }

    static void InspectAscomDevice(ILoggerFactory loggerFactory, string ascomID)
    {
      Console.WriteLine($"Inspecting ASCOM device: {ascomID}");

      var ascomClientlogger = loggerFactory.CreateLogger<AscomClient>();

      var ascomClient = new AscomClient(ascomClientlogger, ascomID);
      for (short switchID = 1; switchID < ascomClient.MaxSwitch; switchID++)
      {
        var switchName = ascomClient.GetSwitchName(switchID);
        var switchValue = ascomClient.GetSwitchValue(switchID);
        Console.WriteLine($"Switch {switchID} name is {switchName}.  Value is {switchValue}");
      }
    }
  }
}
