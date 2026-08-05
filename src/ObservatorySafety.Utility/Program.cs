using Microsoft.Extensions.Logging;

using ObservatorySafety.Infrastructure.ASCOM;

using System.CommandLine;

namespace ObservatoryUtility
{
  public class Program
  {
    static async Task Main(string[] args)
    {
      // GLOBAL HELP: "<exe> help" or "<exe> --help"
      if (args.Length > 0 &&
          (args[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
           args.Contains("--help")))
      {
        ShowHelp();
        return;
      }
        
      var loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddSimpleConsole(options =>
        {
          options.SingleLine = true;
          options.TimestampFormat = "HH:mm:ss ";
        });
      });

      // ---------------------------------------------------------
      // ROOT COMMAND
      // ---------------------------------------------------------
      var root = new RootCommand("ObservatorySafety Utility");

      // ---------------------------------------------------------
      // ASCOM COMMAND
      // ---------------------------------------------------------
      var ascomCommand = new Command("ascom", "Inspect or control an ASCOM device");

      // REQUIRED: Your option format
      var nameOption = new Option<string>("--name")
      {
        Description = "ASCOM ProgID of the device"
      };

      var switchIndexOption = new Option<int?>("--switch")
      {
        Description = "Switch index to set (optional)"
      };

      var switchNameOption = new Option<string?>("--switch-name")
      {
        Description = "Switch name to set (optional)"
      };

      var valueOption = new Option<int?>("--value")
      {
        Description = "Value to set (0 or 1)"
      };

      ascomCommand.Options.Add(nameOption);
      ascomCommand.Options.Add(switchIndexOption);
      ascomCommand.Options.Add(switchNameOption);
      ascomCommand.Options.Add(valueOption);

      root.Subcommands.Add(ascomCommand);

      // ---------------------------------------------------------
      // PARSE
      // ---------------------------------------------------------
      var parseResult = root.Parse(args);

      try
      {
        if (parseResult.CommandResult.Command == ascomCommand)
        {
          var deviceName = parseResult.GetValue(nameOption);
          var switchIndex = parseResult.GetValue(switchIndexOption);
          var switchName = parseResult.GetValue(switchNameOption);
          var value = parseResult.GetValue(valueOption);

          if (deviceName == null)
          {
            Console.WriteLine("ERROR: --name <ProgID> is required.");
            return;
          }

          // -----------------------------------------------------
          // SET SWITCH VALUE
          // -----------------------------------------------------
          if (value != null)
          {
            if (value != 0 && value != 1)
            {
              Console.WriteLine("ERROR: --value must be 0 or 1.");
              return;
            }

            if (switchIndex == null && switchName == null)
            {
              Console.WriteLine("ERROR: You must specify either --switch <index> or --switch-name <name>.");
              return;
            }

            SetAscomSwitch(loggerFactory, deviceName, switchIndex, switchName, value.Value);
            return;
          }

          // -----------------------------------------------------
          // DEFAULT: INSPECT DEVICE
          // -----------------------------------------------------
          InspectAscomDevice(loggerFactory, deviceName);
        }        
        else
        {
          Console.WriteLine("No valid command provided. Use --help for usage information.");
        }
      }
      finally
      {
        // Allow console logger to flush
        await Task.Delay(500);
        loggerFactory.Dispose();
      }
    }

    // ---------------------------------------------------------
    // INSPECT DEVICE
    // ---------------------------------------------------------
    static void InspectAscomDevice(ILoggerFactory loggerFactory, string ascomID)
    {
      Console.WriteLine($"Inspecting ASCOM device: {ascomID}");

      var ascomClientLogger = loggerFactory.CreateLogger<AscomClient>();
      var ascomClient = new AscomClient(ascomClientLogger, ascomID);

      var maxSwitch = ascomClient.MaxSwitch;
      Console.WriteLine($"ASCOM device has {maxSwitch} max switches.");

      for (short switchID = 0; switchID < maxSwitch; switchID++)
      {
        var switchName = ascomClient.GetSwitchName(switchID);
        var switchValue = ascomClient.GetSwitchValue(switchID);
        Console.WriteLine($"Switch {switchID} name is {switchName}.  Value is {switchValue}");
      }
    }

    // ---------------------------------------------------------
    // SET SWITCH VALUE
    // ---------------------------------------------------------
    static void SetAscomSwitch(
      ILoggerFactory loggerFactory,
      string ascomID,
      int? switchIndex,
      string? switchName,
      int value)
    {
      Console.WriteLine($"Setting ASCOM switch on {ascomID}...");

      var ascomClientLogger = loggerFactory.CreateLogger<AscomClient>();
      var ascomClient = new AscomClient(ascomClientLogger, ascomID);

      short index = -1;

      if (switchIndex != null)
      {
        index = (short) switchIndex.Value;
      }
      else if (switchName != null)
      {
        var maxSwitch = ascomClient.MaxSwitch;

        for (short i = 0; i < maxSwitch; i++)
        {
          if (string.Equals(ascomClient.GetSwitchName(i), switchName, StringComparison.OrdinalIgnoreCase))
          {
            index = i;
            break;
          }
        }

        if (index < 0)
        {
          Console.WriteLine($"ERROR: Switch name '{switchName}' not found.");
          return;
        }
      }

      ascomClient.SetSwitchValue(index, value == 1);
      Console.WriteLine($"Switch {index} set to {value}.");
    }

    static void ShowHelp()
    {
      Console.WriteLine();
      Console.WriteLine("Command Help");
      Console.WriteLine("------------");
      Console.WriteLine("Inspect or control an ASCOM device.");
      Console.WriteLine();
      Console.WriteLine("Usage:");
      Console.WriteLine("  ascom --name <ProgID>");
      Console.WriteLine("      Inspect the ASCOM device and list all switches.");
      Console.WriteLine();
      Console.WriteLine("  ascom --name <ProgID> --switch <index> --value 0|1");
      Console.WriteLine("      Set a switch by numeric index.");
      Console.WriteLine();
      Console.WriteLine("  ascom --name <ProgID> --switch-name <name> --value 0|1");
      Console.WriteLine("      Set a switch by its friendly name.");
      Console.WriteLine();
      Console.WriteLine("Options:");
      Console.WriteLine("  --name <ProgID>         Required. ASCOM device ProgID.");
      Console.WriteLine("  --switch <index>        Optional. Switch index to set.");
      Console.WriteLine("  --switch-name <name>    Optional. Switch name to set.");
      Console.WriteLine("  --value <0|1>           Optional. Value to set (0 or 1).");
      Console.WriteLine();
      Console.WriteLine("Examples:");
      Console.WriteLine("  ascom --name Dragonfly.Dome");
      Console.WriteLine("  ascom --name Dragonfly.Dome --switch 5 --value 1");
      Console.WriteLine("  ascom --name Dragonfly.Dome --switch-name \"Shutter\" --value 0");
      Console.WriteLine();

    }
  }
}
