namespace ObservatorySafety.Core;

public record ShutdownCommand(
    bool AbortCameraExposure,
    bool AbortSequence,
    bool StopSequence,
    bool ParkMount,
    bool WarmCamera,
    bool CloseDome
);
