namespace OrbitBubble.Domain.Gestures;

public sealed class CircleGestureDetector : IGestureDetector {
  public GestureResult Detect(IReadOnlyList<TimedPoint> samples, GestureOptions options, out GestureDebugInfo debug) {
    debug = default;

    if (samples.Count < options.MinPoints)
      return GestureResult.None;

    // 1) center = mean(x), mean(y)
    double cx = 0, cy = 0;
    for (int i = 0; i < samples.Count; i++) {
      cx += samples[i].X;
      cy += samples[i].Y;
    }
    cx /= samples.Count;
    cy /= samples.Count;

    // 2) radii stats
    double sumR = 0;
    double sumR2 = 0;

    // 3) angle coverage
    double minAngle = double.PositiveInfinity;
    double maxAngle = double.NegativeInfinity;

    for (int i = 0; i < samples.Count; i++) {
      var dx = samples[i].X - cx;
      var dy = samples[i].Y - cy;

      var r = Math.Sqrt(dx * dx + dy * dy);
      sumR += r;
      sumR2 += r * r;

      var a = Math.Atan2(dy, dx); // [-pi, pi]
      if (a < minAngle) minAngle = a;
      if (a > maxAngle) maxAngle = a;
    }

    var meanR = sumR / samples.Count;
    if (meanR < options.MinRadiusDip || meanR > options.MaxRadiusDip)
      return GestureResult.None;

    var variance = (sumR2 / samples.Count) - (meanR * meanR);
    var stdDev = variance > 0 ? Math.Sqrt(variance) : 0;
    var stdRatio = meanR > 0 ? (stdDev / meanR) : 1;

    // 角度覆蓋：max-min 但注意穿越 -pi/pi 的情況
    // 用簡化處理：把角度投到 [0, 2pi) 再算覆蓋
    double minA = double.PositiveInfinity, maxA = double.NegativeInfinity;
    for (int i = 0; i < samples.Count; i++) {
      var dx = samples[i].X - cx;
      var dy = samples[i].Y - cy;
      var a = Math.Atan2(dy, dx);
      if (a < 0) a += (Math.PI * 2);
      if (a < minA) minA = a;
      if (a > maxA) maxA = a;
    }
    var coverage = (maxA - minA) / (Math.PI * 2);

    debug = new GestureDebugInfo(
        PointCount: samples.Count,
        CenterX: cx,
        CenterY: cy,
        MeanRadius: meanR,
        RadiusStdDevRatio: stdRatio,
        AngleCoverage: coverage);

    if (coverage >= options.AngleCoverageThreshold &&
        stdRatio <= options.RadiusStdDevRatioMax) {
      return GestureResult.Circle;
    }

    return GestureResult.None;
  }
}
