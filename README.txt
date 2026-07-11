ObservatorySafety.Service
=========================
To run as application:
ObservatorySafety.Service.exe --console

To run as service:
sc create ObservatorySafetyService binPath= "C:\Path\ObservatorySafety.Service.exe"
sc start ObservatorySafetyService

Other flags:
--dry-run : Run without making any NINA calls (for testing purposes).
--simulate-power-loss : Simulate a power loss event (for testing purposes).

ObservatorySafety.Watchdog
=========================
To run as application:
ObservatorySafety.Watchdog.exe --console

To run as service:
sc create ObservatorySafetyWatchdog binPath= "C:\Path\ObservatorySafety.Watchdog.exe"
sc start ObservatorySafetyWatchdog

IMPORTANT:
After installing the Watchdog service, you must amend the appsettings.json file to have the correct alert services enables (with their credential fields populated).
Or a better solution is to add a new appsettings.PRODUCTION.json file with the correct settings and then set the environment variable DOTNET_ENVIRONMENT=PRODUCTION for the service to pick up the correct settings.
You must update the "MainServiceLogDirectory" property in the appsettings.json file to point to the same log directory as the ObservatorySafety.Service service. Otherwise, the Watchdog service will not be able to find the log files for the main service and will not be able to send alerts.

LOG FILE LOCATION
=================
When you install and run these applications as Windows Services the log file root with be "C:\Windows\System32" which may not suit you. 
If you want the logs files to be in a different folder (same folder as the executable) then modify the appsettings.json file with the full log path,
rather than just "logs" (which is relative to the current working directory). For example, change "logs" to "C:\Path\logs" in the appsettings.json file.
After updating the json file, you will need to restart the service for the changes to take effect.