namespace ObservatorySafety.Infrastructure;

public class SafetyOptions
{
  public string FlagFilePath { get; set; } = "";
  public int DebounceSeconds { get; set; } = 30;
}
