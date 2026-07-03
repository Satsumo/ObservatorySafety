public class SafetyOptions
{
  public string FlagFilePath { get; set; } = "";
  public int DebounceSeconds { get; set; }

  public string GetExpandedFlagFilePath()
  {
    return Environment.ExpandEnvironmentVariables(FlagFilePath);
  }
}
