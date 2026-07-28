ObservatorySafety.Service
=========================
This application attempts to provide the complete astronomical observatory safety system.
It polls a number of status providers for key data, such as "is the weather safe" or "is the power down" etc.
Then there a number of handlers that respond to the data changes captured by the status providers.  These 
handlers can be anything from "send me an email if the power goes down" or something more complicated such
as "if the astronomy application responsible for ensuring the roof is closed in bad weather is not running then
trigger the motor controller to close the roof".

The architecture is designed to be flexible and easily support different hardware configurations, but "in the box"
it is designed as follows:
1. The astronomy application is NINA - we query NINA for the following information:
		- Is the application responsive
		- Is a sequence running
		- Is the mount connected
		- Is the safety monitor (the weather monitor) connected
2. The weather monitor is a Lunatico CloudWatcher - we query it for the state of the weather, safe or not safe.
3. The roof monitor is a Dragonfly V2 - we query it to determine whether the roof is open or closed.
4. The mount monitor is a Dark Dragons Mount Park Sensor - we query it to determine whether the mount is parked or unparked.

Note that we can obviously get some of this information from different sources, for example NINA "could" tell us if the mount 
is parked or not, but that isn't 100% guaranteed to be correct or possible, hence we go to the cast iron source; the mount sensor.

The handlers supplied are as follows:
1. Notification - this will notify me (via Email, WhatsApp or PushOver) of all the statuses that I configure as being important. It can handle:
		- The power is out.
		- The weather is bad and the roof is open.
		- NINA is not running.
2. USB Relay Switch - I have a 4-channel USB Relay Switch that is connected to the Dragonfly. The aim is to use switches to keep the Dragonfly
   aware of the complete status of the observatory.  It can then react accordingly.  The switches I am using are as follows (CloudWatcher is already
	 connected to the Dragonfly hence we don't need to do anything with that):
	  - Power Status (On = power good)
		- Mount Parked (On = parked)
		- NINA Running (On = running)
		- NINA Sequence Running (On = sequence running)
3. NINA - If the roof is open then we expect a sequence to be running in NINA.  The sequence is coded to handle the bad weather state, but if
   a sequence is not running then there's nothing that will react to a bad weather state.  Also NINA isn't able to monitor power outages, so it
	 cannot handle them (even if a sequence is running).  Therefore we monitor power and weather and whether NINA has a sequence running, so that
	 we can intervene and manually ask NINA to shutdown the observatory (stop sequence, park the mount and close the roof).

Note: We could also have a Dragonfly handler that instructs it to close the roof if certain conditions occur (bad weather but NINA isn't running)
      but we have passed all the critical information to the Dragonfly via the USB Relay Switch, hence that enables us to write a macro within
			Dragonfly so that it can decide when it needs to step in a close the roof because no-one else can be trusted to do it.

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