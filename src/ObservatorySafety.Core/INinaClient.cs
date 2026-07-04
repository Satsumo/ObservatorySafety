namespace ObservatorySafety.Core;

public interface INinaClient
{
  const string API_BASE = "/api/v2";
  const string API_ABORT_CAMERA_EXPOSURE = $"{API_BASE}/equipment/camera/abort-exposure";
  const string API_ABORT_SEQUENCE = $"{API_BASE}/sequences/abort";
  const string API_STOP_SEQUENCE = $"{API_BASE}/sequences/stop";  
  const string API_PARK_MOUNT = $"{API_BASE}/mount/park";
  const string API_WARM_CAMERA = $"{API_BASE}/camera/warm";
  const string API_CLOSE_DOME = $"{API_BASE}/dome/close";
  const string API_EXECUTE_SHUTDOWN = $"{API_BASE}/shutdown";
  Task AbortCameraExposureAsync();
  Task AbortSequenceAsync();
  Task StopSequenceAsync();
  Task ParkMountAsync();
  Task WarmCameraAsync();
  Task CloseDomeAsync();

  // Added for shutdown orchestration
  Task ExecuteShutdownAsync(ShutdownCommand cmd);
}
