using ObservatorySafety.Core.Model;

namespace ObservatorySafety.Core;

public interface INinaClient
{
  const string API_BASE = "/v2/api";

  const string API_EQUIPMENT_INFO = $"{API_BASE}/equipment/info";

  const string API_STOP_SEQUENCE = $"{API_BASE}/sequence/stop";
  const string API_PARK_MOUNT = $"{API_BASE}/equipment/mount/park";
  const string API_WARM_CAMERA = $"{API_BASE}/equipment/camera/warm";
  const string API_CLOSE_DOME = $"{API_BASE}/equipment/dome/close";

  Task<EquipmentInfoEnvelope> GetEquipmentInfoAsync();

  Task StopSequenceAsync();

  Task ParkMountAsync();

  Task WarmCameraAsync();

  Task CloseDomeAsync();

  // Added for shutdown orchestration
  Task ExecuteShutdownAsync(ShutdownCommand cmd);
}
