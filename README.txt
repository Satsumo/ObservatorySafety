
To run as application:
ObservatorySafety.Service.exe --console

To run as service:
sc create ObservatorySafetyService binPath= "C:\Path\ObservatorySafety.Service.exe"
sc start ObservatorySafetyService


Also can use alternative configration (for different observatory):
dotnet run --console --config mysettings.json
or
ObservatorySafety.Service.exe --console --config C:\obs\config.json

Other flags:
--dry-run : Run without making any NINA calls (for testing purposes).
--simulate-power-loss : Simulate a power loss event (for testing purposes).

