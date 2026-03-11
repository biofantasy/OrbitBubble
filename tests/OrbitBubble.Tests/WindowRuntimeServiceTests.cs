using OrbitBubble.Core.Services;
using System.Windows;

namespace OrbitBubble.Tests;

public class WindowRuntimeServiceTests {
  [Fact]
  public void CalculateWrapperPosition_AlignsOrbitCenterToMouseWithoutOffsetDrift() {
    var service = new WindowRuntimeService();
    var virtualOrigin = new Point(0, 0);

    // 模擬中心被拖到舞台右側 50，下一次開啟仍要能精準對準滑鼠
    var orbitCenter = new Point(450, 400);
    var mouse = new Point(1200, 700);

    var wrapper = service.CalculateWrapperPosition(mouse, virtualOrigin, orbitCenter);

    // 驗證：wrapper + orbitCenter = mouse，代表不會再有累積偏移
    Assert.Equal(mouse.X, wrapper.X + orbitCenter.X, 3);
    Assert.Equal(mouse.Y, wrapper.Y + orbitCenter.Y, 3);
  }
}
