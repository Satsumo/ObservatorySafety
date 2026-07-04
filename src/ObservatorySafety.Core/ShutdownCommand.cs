namespace ObservatorySafety.Core;

public record ShutdownCommand(
    bool StopSequence,
    bool ParkMount,
    bool WarmCamera,
    bool CloseDome
);
