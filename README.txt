
*****************************
*                           *
* ObservatorySafety.Service *
*                           *
*****************************
This application attempts to provide the complete astronomical observatory safety system.
The concept is simple; the service polls a number of Monitors.  Each monitor provides one of more status values.
We, then, also have a number of Handlers; these react to status value changes.  They are only triggered if one or more of
the monitors report a status value change.

The monitors are configurable in terms of which status values they're allowed to provide.  This is because some status values
can come from multiple monitors.  For example, whether the telescope mount is parked on not can, in my setup, come from NINA, a mount 
sensor or a Dragonfly.  The most reliable source for that status is my Dark Dragon Mount Sensor, hence that is the only monitor that
is comfigured to provide status MountParked.

You get the idea!

My monitor setup consists of the following:
- Lunitico CloudWatcher - As well as providing weather data it also provides the "IsSafe" value.  The CloudWatcher sensor is 
	hooked directly into my Dragonfly, so it too is a reliable source for "IsSafe", so I get the "WeatherSafe" status from Dragonfly.
	I use the CloudWatcher Monitor to just monitor the data file it generates (which is important to NINA); I check that it isn't state,
	which would happen if the AAG CloudWatcher application crashed/was not running.
- DarkDragon Mount Sensor - This is a tilt sensor.  It is the most reliable way of knowing whether my telescope mount is parked or not,
	hence this monitor is used to provide the "MountParked" status.
- Lunitico Dragonfly - I use the Dragonfly primarily to control the roof open and close operations.  The "RoofOpened" and "RoofClosed" sensors 
	directly feed into the Dragonfly, hence it is the provider for those two statuses.  The configuration determines which status values
	I read off the Dragonfly, i.e. what each sensor on the Dragonfly relates to.
- Power - my setup is backed up by a UPS, which is connected to my Observatory PC via USB.  I have a monitor that can poll the PC to
  check whether it is on mains or battery, hence allowing me to provide "PowerOn" status.  Note that the monitor is designed to only send a
	"PowerOn" false if the power switches to battery for longer than a configured amount of seconds - this is so that we don't worry about
	short power outages (which the UPS can easily handle).
- Observatory PC - this is the PC that connects to all my hardware and runs NINA.  The PC itself doesn't matter; what matters is 
	whether NINA is running or not.  In my setup it should always be running (it's setup to auto start on PC restart).  I therefore
	have a NINA monitor.  It tells me whether NINA is running and whether a Sequence is currently running in NINA.  
	Sequence monitoring is important because all my sequences are designed to close the roof if weather is not safe. This means I can
	(mostly) ignore unsafe status if a Sequence is running because I know it should close the roof.

That is my monitoring in a nutshell.  The next part is the Handlers.
- Notification - this handler sends me alerts if certain status values are encountered. Currently the code supports, WhatsApp (via 
	Twilio), PushOver and Email alerts.  These are all configurable.  For example, I send alerts to myself if the "PowerOn" status ever
	goes false.
- USB Relay Switch - I wanted the Dragonfly to be aware of more values than I can hardwire into its sensors.  For example, I want the
	Dragonfly to know whether a Sequence is running or not in NINA.  The aim is that I can have a macro script running on the Dragonfly
	that acts as a failsafe - allowing/guiding the Dragonfly to close the roof if every other failsafe fails.  I have the four channels
	on this USB Relay Switch hardwired to the Dragonfly.  The configuration determines what statuses are fed to these channels hence
	controlling what I inform the Dragonfly of.
- Shutdown - this will examine certain status values (configured) and if ALL of them are false (for example, the roof is not closed
	and the weather is not safe) then it will start a shutdown sequence (which will only do anything if the shutdown state remains in place
	for a configured number of minutes.  The shutdown process is also based on configuration, and currently only works on ASCOM targets.
	It will loop through the configured ASCOM targets and set their configured switches to 1 (on).

Starting
========
You can run the safety service in a console rather than as (the installed) service by running:
ObservatorySafety.Service.exe --console

Other flags:
--dry-run : Run without making any NINA calls (for testing purposes).
--simulate-power-loss : Simulate a power loss event (for testing purposes).


******************************
*                            *
* ObservatorySafety.Watchdog *
*                            *
******************************

This watchdog service simply keeps an eye on the safety service, since it is so crucial.  It monitors the safetys service's log file and reacts to certain
keywords (configured) and sents alerts if the keyword(s) are encountered.  Just like the safety service, the supported alerts are configurables (WhatsApp, PushOver
and Email).

Starting ObservatorySafety.Watchdog
===================================
You can run the watchdog service in a console rather than as (the installed) service by running:
ObservatorySafety.Watchdog.exe --console


****************
*              *
* INSTALLATION *
*              *
****************
After installing either service, you must ADD an appsettings.PRODUCTION.json file.  Do not modify the appsettings.json file as it will get overwritten every release.
Ensure your appsettings.PRODUCTION.json file has all the correct alert services enabled (with their credential fields populated).  Also add any property overrides in this
file for the other settings (for example, which monitor provides which statuses, as you setup may well be different from mine/default).  You only need to put the
overrides in the appsettings.PRODUCTION.json file it will get merged on top of the default (installed) appsettings.json file.
Ensure environment variable is set as DOTNET_ENVIRONMENT=PRODUCTION for the service to pick up the correct settings.
You must update the "MainServiceLogDirectory" property in you Watchdog service's json file to point to the same log directory as the ObservatorySafety.Service 
service, otherwise the Watchdog service will not be able to find the log files for the main service and will not be able to send alerts.

Note that installation does NOT restart the service (because we want to give you a chance to modify your appsettings).  Reboot of the PC will restart the services (or
you can manually do it from the Services app).

*********************
*                   *
* LOG FILE LOCATION *
*                   *
*********************
When you install and run these applications as Windows Services the log file root with be "C:\Windows\System32" which may not suit you. 
If you want the logs files to be in a different folder (same folder as the executable) then modify the logfile setting in your json to the full log path,
rather than just "logs" (which is relative to the current working directory). For example, change "logs" to "C:\Path\logs" in the appsettings.PRODUCTION.json file.
After updating the json file, you will need to restart the service for the changes to take effect.

*********
*       *
* NOTES *
*       *
*********
I am embarrassed to admin that lot of the test classes are missing as I reengineered the system several times.  I will in due course remedy that shocking state of 
affairs.  I am, however, retired and happy, so I tend to do what pleases me. :D
