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
