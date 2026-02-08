namespace OrbitBubble.Domain.Gestures;

public interface IGestureDetector {
  GestureResult Detect(IReadOnlyList<TimedPoint> samples, GestureOptions options, out GestureDebugInfo debugInfo);
}

/// <summary>帶時間戳的點（DIP）</summary>
public readonly record struct TimedPoint(double X, double Y, long TimestampMs);

public readonly record struct GestureDebugInfo(
    int PointCount,
    double CenterX,
    double CenterY,
    double MeanRadius,
    double RadiusStdDevRatio,
    double AngleCoverage);