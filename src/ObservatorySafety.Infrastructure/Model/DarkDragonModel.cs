namespace ObservatorySafety.Infrastructure.Model
{
  // Typical json returned by Dark Dragon status url:
  // {"pitch":5.3619593718765675,"roll":-163.04869264829637,"isSafeToMove":false,"serialNumber":"206EF1A964C8","name":"MountSensor","version":"1.6.0"}
  public class DarkDragonModel
  {

    public double Pitch { get; set; }
    public double Roll { get; set; }
    public bool IsSafeToMove { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;

  }
}
