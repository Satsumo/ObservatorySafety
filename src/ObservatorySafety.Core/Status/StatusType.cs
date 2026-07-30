namespace ObservatorySafety.Core.Status
{
  public enum StatusType
  {
    ApplicationRunning,
    CloudWatcherDataStale,
    MountParked,
    PowerOn,
    RoofClosed,
    RoofOpen,
    SequenceRunning,
    WeatherSafe,
    None
  }
}
