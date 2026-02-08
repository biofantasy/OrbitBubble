namespace OrbitBubble.Domain.Gestures;

public sealed class GestureOptions {

  /// <summary>最多保留幾毫秒的點位（滑鼠畫圈通常 700ms~1500ms）</summary>
  public int WindowMs { get; set; } = 1200;

  /// <summary>最少點數，太少很容易誤判</summary>
  public int MinPoints { get; set; } = 24;

  /// <summary>兩點距離小於此值不收（DIP），避免微抖增加雜訊</summary>
  public double MinDistanceDip { get; set; } = 3.0;

  /// <summary>圈的最小半徑（DIP），太小容易誤觸</summary>
  public double MinRadiusDip { get; set; } = 35.0;

  /// <summary>圈的最大半徑（DIP），太大可能是「亂畫」</summary>
  public double MaxRadiusDip { get; set; } = 450.0;

  /// <summary>角度覆蓋率門檻（0~1），0.85 表示覆蓋 306 度以上</summary>
  public double AngleCoverageThreshold { get; set; } = 0.85;

  /// <summary>半徑變異容忍度，越小越嚴格（0.35~0.55 常用）</summary>
  public double RadiusStdDevRatioMax { get; set; } = 0.50;

  /// <summary>觸發後冷卻時間，避免連續觸發</summary>
  public int CooldownMs { get; set; } = 500;
}

