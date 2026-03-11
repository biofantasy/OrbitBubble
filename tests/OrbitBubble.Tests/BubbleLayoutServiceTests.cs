using OrbitBubble.Core.Services;
using System.Windows;

namespace OrbitBubble.Tests;

public class BubbleLayoutServiceTests {
  [Fact]
  public void CalculateBubblePosition_UsesGivenCenterAndRadius() {
    var service = new BubbleLayoutService();
    var center = new Point(200, 300);

    var p0 = service.CalculateBubblePosition(0, 4, 20, 20, center, 100);
    var p1 = service.CalculateBubblePosition(1, 4, 20, 20, center, 100);

    Assert.Equal(290, p0.X, 3);
    Assert.Equal(290, p0.Y, 3);

    Assert.Equal(190, p1.X, 3);
    Assert.Equal(390, p1.Y, 3);
  }

  [Fact]
  public void CalculateBubblePosition_WhenTotalCountIsZero_ReturnsCenterAlignedPosition() {
    var service = new BubbleLayoutService();
    var center = new Point(500, 600);

    var result = service.CalculateBubblePosition(0, 0, 40, 60, center, 180);

    Assert.Equal(480, result.X, 3);
    Assert.Equal(570, result.Y, 3);
  }
}
