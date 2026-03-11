using System.Windows;

namespace OrbitBubble.Core.Services;

public class BubbleLayoutService {
  public const double DefaultCenterX = 400;
  public const double DefaultCenterY = 400;
  public const double DefaultRadius = 180;

  /// <summary>
  /// 根據索引與總數，回傳泡泡左上角座標。
  /// </summary>
  public Point CalculateBubblePosition(int index, int totalCount, double bubbleWidth, double bubbleHeight) {
    return CalculateBubblePosition(
      index,
      totalCount,
      bubbleWidth,
      bubbleHeight,
      new Point(DefaultCenterX, DefaultCenterY),
      DefaultRadius);
  }

  public Point CalculateBubblePosition(
    int index,
    int totalCount,
    double bubbleWidth,
    double bubbleHeight,
    Point orbitCenter,
    double orbitRadius = DefaultRadius) {
    if (totalCount <= 0) {
      return new Point(orbitCenter.X - (bubbleWidth / 2), orbitCenter.Y - (bubbleHeight / 2));
    }

    double angle = index * Math.PI * 2 / totalCount;
    double x = orbitCenter.X + orbitRadius * Math.Cos(angle) - (bubbleWidth / 2);
    double y = orbitCenter.Y + orbitRadius * Math.Sin(angle) - (bubbleHeight / 2);
    return new Point(x, y);
  }
}
