namespace ObservatorySafety.Infrastructure.ASCOM
{
  public interface IAscomClient
  {
    bool IsConnected { get; }

    bool GetSwitchValue(short switchID);

    void SetSwitchValue(short switchID, bool value);
  }
}
