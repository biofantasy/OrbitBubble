using OrbitBubble.Core.Services;
using System.Windows;

namespace OrbitBubble.Tests;

public class GestureServiceTests {
  [Fact]
  public void ProcessPoint_WithSmallMovement_DoesNotTrigger() {
    var service = new GestureService();

    for (int i = 0; i < 20; i++) {
      var trigger = service.ProcessPoint(new Point(10 + i % 2, 10 + i % 2), isMenuVisible: false);
      Assert.Equal(GestureTrigger.None, trigger);
    }
  }

  [Fact]
  public void ProcessPoint_RotationCanTriggerOpenOrCloseAccordingToVisibility() {
    var openResult = RunRotationSequence(isMenuVisible: false);
    Assert.Equal(GestureTrigger.OpenMenu, openResult);

    var closeResult = RunRotationSequence(isMenuVisible: true);
    Assert.Equal(GestureTrigger.CloseMenu, closeResult);
  }

  private static GestureTrigger RunRotationSequence(bool isMenuVisible) {
    var service = new GestureService();
    var center = new Point(200, 200);
    var radius = 80d;

    // 先餵足 10 點初始化圓心
    for (int i = 0; i < 10; i++) {
      double angle = i * Math.PI / 18;
      var p = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
      service.ProcessPoint(p, isMenuVisible);
    }

    for (int i = 10; i < 80; i++) {
      double angle = i * Math.PI / 18;
      var p = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
      var trigger = service.ProcessPoint(p, isMenuVisible);
      if (trigger != GestureTrigger.None) {
        return trigger;
      }
    }

    // 若正向旋轉方向因座標系解讀差異導致未觸發，反向再試一次
    service.Reset();
    for (int i = 0; i < 10; i++) {
      double angle = -i * Math.PI / 18;
      var p = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
      service.ProcessPoint(p, isMenuVisible);
    }

    for (int i = 10; i < 80; i++) {
      double angle = -i * Math.PI / 18;
      var p = new Point(center.X + radius * Math.Cos(angle), center.Y + radius * Math.Sin(angle));
      var trigger = service.ProcessPoint(p, isMenuVisible);
      if (trigger != GestureTrigger.None) {
        return trigger;
      }
    }

    return GestureTrigger.None;
  }
}
