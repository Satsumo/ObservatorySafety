namespace ObservatorySafety.Core;

public class EquipmentOptions
{
  public int MountParkTimeThresholdSeconds { get; set; }

  public int DomeCloseTimeThresholdSeconds { get; set; }

  public int PowerOutagePollingTimeSeconds { get; set; }

  public int PowerOutageConfirmedThresholdSeconds { get; set; }
}
