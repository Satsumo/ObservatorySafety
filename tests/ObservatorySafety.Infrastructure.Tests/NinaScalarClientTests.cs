using System.Net;

using Moq;

using ObservatorySafety.Core;
using ObservatorySafety.Core.Model;

namespace ObservatorySafety.Infrastructure.Tests;

[TestFixture]
public class NinaScalarClientTests
{
  private Mock<IHttpService> _mockHttpService;
  private NinaScalarClient _client;

  [SetUp]
  public void Setup()
  {
    _mockHttpService = new Mock<IHttpService>(MockBehavior.Strict);

    var equipmentOptions = new EquipmentOptions
    {
      DomeCloseTimeThresholdSeconds = 2,
      MountParkTimeThresholdSeconds = 2
    };

    _client = new NinaScalarClient(_mockHttpService.Object, equipmentOptions);
  }

  [Test]
  public async Task CallsCorrectEndpoints_ForShutdown()
  {
    var envelope = new EquipmentInfoEnvelope
    {
      Response = new EquipmentInfo
      {
        Camera = new EquipmentCameraInfo { CoolerOn = false },
        Dome = new EquipmentDomeInfo { ShutterStatus = "ShutterClosed", Slewing = false, AtPark = true, Connected = true },
        Mount = new EquipmentMountInfo { AtPark = true, Slewing = false, TrackingEnabled = false, Connected = true },
        Sequence = new EquipmentSequenceInfo { IsRunning = false },
        SafetyMonitor = new EquipmentSafetyMonitorInfo { IsSafe = true }
      },
      Error = null,
      StatusCode = 200,
      Success = true,
      Type = "EquipmentInfo"
    };
    HttpResponseMessage equipmentResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
      Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(envelope))
    };  

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_EQUIPMENT_INFO))
      .ReturnsAsync(equipmentResponse);

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    await _client.ExecuteShutdownAsync(new ShutdownCommand(true, true, true, true));

    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME), Times.Exactly(1));

  }

  [Test]
  public void ShutdownSequence_ThrowsOnNonSuccessStatusCode()
  {
    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION))
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE))
        .Throws(new Exception("Failed to call stop sequence endpoint"));

    var ex = Assert.ThrowsAsync<Exception>(async () =>
        await _client.ExecuteShutdownAsync(new ShutdownCommand(true, true, true, true))
    );

    Assert.That(ex.Message, Is.EqualTo("Failed to call stop sequence endpoint"));

    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT), Times.Never());
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA), Times.Never());
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME), Times.Never());
  }

  [Test]
  public void ShutdownSequence_FailsIfMountFailsToPark()
  {
    var envelope = new EquipmentInfoEnvelope
    {
      Response = new EquipmentInfo
      {
        Camera = new EquipmentCameraInfo { CoolerOn = false },
        Dome = new EquipmentDomeInfo { ShutterStatus = "ShutterClosed", Slewing = false, AtPark = true, Connected = true },
        Mount = new EquipmentMountInfo { AtPark = false, Slewing = false, TrackingEnabled = false, Connected = true },
        Sequence = new EquipmentSequenceInfo { IsRunning = false },
        SafetyMonitor = new EquipmentSafetyMonitorInfo { IsSafe = true }
      },
      Error = null,
      StatusCode = 200,
      Success = true,
      Type = "EquipmentInfo"
    };
    HttpResponseMessage equipmentResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
      Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(envelope))
    };

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_EQUIPMENT_INFO))
      .ReturnsAsync(equipmentResponse);

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    var ex = Assert.ThrowsAsync<Exception>(async () =>
        await _client.ExecuteShutdownAsync(new ShutdownCommand(true, true, true, true))
    );

    Assert.That(ex.Message, Is.EqualTo("MOUNT PARK FAILURE: Mount did not park or it is still slewing/tracking"));

    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA), Times.Never);
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME), Times.Never);
  }


  [Test]
  public async Task ShutdownSequence_SuccessIfMountNotConnected()
  {
    var envelope = new EquipmentInfoEnvelope
    {
      Response = new EquipmentInfo
      {
        Camera = new EquipmentCameraInfo { CoolerOn = false },
        Dome = new EquipmentDomeInfo { ShutterStatus = "ShutterClosed", Slewing = false, AtPark = true, Connected = true },
        Mount = new EquipmentMountInfo { AtPark = false, Slewing = false, TrackingEnabled = false, Connected = false },
        Sequence = new EquipmentSequenceInfo { IsRunning = false },
        SafetyMonitor = new EquipmentSafetyMonitorInfo { IsSafe = true }
      },
      Error = null,
      StatusCode = 200,
      Success = true,
      Type = "EquipmentInfo"
    };
    HttpResponseMessage equipmentResponse = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
    {
      Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(envelope))
    };

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_EQUIPMENT_INFO))
      .ReturnsAsync(equipmentResponse);

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    _mockHttpService.Setup(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME))
      .ReturnsAsync(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    await _client.ExecuteShutdownAsync(new ShutdownCommand(true, true, true, true));

    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_VERSION), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_STOP_SEQUENCE), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_PARK_MOUNT), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_WARM_CAMERA), Times.Exactly(1));
    _mockHttpService.Verify(s => s.Call(HttpMethod.Get, IAstronomyApplicationClient.API_CLOSE_DOME), Times.Exactly(1));
  }

}
