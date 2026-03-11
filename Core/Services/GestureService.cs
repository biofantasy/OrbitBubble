using System.Windows;

namespace OrbitBubble.Core.Services;

public enum GestureTrigger {
  None,
  OpenMenu,
  CloseMenu
}

public class GestureService {
  private readonly List<Point> _points = new();
  private Point? _center;
  private Point _lastPoint;
  private double _accumulatedAngle;

  /// <summary>
  /// 處理一筆滑鼠座標，並回傳是否觸發手勢命令。
  /// </summary>
  public GestureTrigger ProcessPoint(Point currentPos, bool isMenuVisible) {
    _points.Add(currentPos);

    // 點數足夠後固定圓心，避免圓心跟著滑鼠漂移
    if (_points.Count == 10) {
      _center = new Point(_points.Average(p => p.X), _points.Average(p => p.Y));
      _lastPoint = currentPos;
      return GestureTrigger.None;
    }

    if (_points.Count < 10 || _center == null) {
      _lastPoint = currentPos;
      return GestureTrigger.None;
    }

    var center = _center.Value;
    var lastVector = _lastPoint - center;
    var currentVector = currentPos - center;

    // 過濾原地微動，避免誤判為旋轉
    if (currentVector.Length < 30) {
      _lastPoint = currentPos;
      return GestureTrigger.None;
    }

    double angleLast = Math.Atan2(lastVector.Y, lastVector.X);
    double angleCurrent = Math.Atan2(currentVector.Y, currentVector.X);
    double deltaAngle = (angleCurrent - angleLast) * (180 / Math.PI);

    if (deltaAngle > 180) deltaAngle -= 360;
    if (deltaAngle < -180) deltaAngle += 360;

    if (Math.Abs(deltaAngle) > 0.5) {
      _accumulatedAngle += deltaAngle;
    }

    GestureTrigger trigger = GestureTrigger.None;
    if (Math.Abs(_accumulatedAngle) > 130) {
      bool isClockwise = _accumulatedAngle > 0;
      if (isClockwise && !isMenuVisible) {
        trigger = GestureTrigger.OpenMenu;
      } else if (!isClockwise && isMenuVisible) {
        trigger = GestureTrigger.CloseMenu;
      }
    }

    if (trigger != GestureTrigger.None || _points.Count > 50) {
      Reset();
      return trigger;
    }

    _lastPoint = currentPos;
    return GestureTrigger.None;
  }

  public void Reset() {
    _points.Clear();
    _center = null;
    _accumulatedAngle = 0;
  }
}
