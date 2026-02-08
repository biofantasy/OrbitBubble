using Microsoft.Extensions.Logging;
using OrbitBubble.Domain.Gestures;
using OrbitBubble.Infrastructure.Input;
using System.Diagnostics;

namespace OrbitBubble.Infrastructure.Gestures;

public sealed class GestureDetectionService : IGestureDetectionService {
  private readonly ILogger<GestureDetectionService> _logger;
  private readonly IInputTriggerService _input;
  private readonly IGestureDetector _detector;
  private readonly GestureOptions _options;

  // ring buffer（簡化用 List + Trim）
  private readonly List<TimedPoint> _buffer = new(capacity: 128);

  private long _lastTriggerMs = -1;

  // MinDistanceDip 過濾
  private double _lastX = double.NaN;
  private double _lastY = double.NaN;

  // ✅ 防重入：0=未跑，1=正在跑
  private int _detectGate = 0;

  // 統計
  private readonly Stopwatch _statSw = Stopwatch.StartNew();
  private int _checks;
  private int _hits;
  private long _totalCostMs;
  private GestureDebugInfo _lastDebug;

  public GestureDetectionService(
      ILogger<GestureDetectionService> logger,
      IInputTriggerService input,
      IGestureDetector detector,
      GestureOptions options) {
    _logger = logger;
    _input = input;
    _detector = detector;
    _options = options;
  }

  public void Start() {
    _logger.LogInformation("GestureDetectionService started. windowMs={WindowMs} minPoints={MinPoints} minDist={MinDist} coverTh={CoverTh} stdMax={StdMax} cooldownMs={Cooldown}",
        _options.WindowMs, _options.MinPoints, _options.MinDistanceDip, _options.AngleCoverageThreshold, _options.RadiusStdDevRatioMax, _options.CooldownMs);

    _input.PointerSampled += OnPointerSampled;
  }

  public void Stop() {
    _input.PointerSampled -= OnPointerSampled;
    _buffer.Clear();
    _logger.LogInformation("GestureDetectionService stopped.");
  }

  public void Dispose() => Stop();

  private void OnPointerSampled(object? sender, PointerSampledEventArgs e) {
    var nowMs = Environment.TickCount64;

    // cooldown：剛觸發完先不要再判斷
    if (_lastTriggerMs > 0 && (nowMs - _lastTriggerMs) < _options.CooldownMs)
      return;

    // MinDistanceDip：太近的點不收
    if (!double.IsNaN(_lastX)) {
      var dx = e.XDip - _lastX;
      var dy = e.YDip - _lastY;
      if ((dx * dx + dy * dy) < (_options.MinDistanceDip * _options.MinDistanceDip))
        return;
    }

    _lastX = e.XDip;
    _lastY = e.YDip;

    _buffer.Add(new TimedPoint(e.XDip, e.YDip, nowMs));
    TrimOld(nowMs);

    // 點數不足就先不跑
    if (_buffer.Count < _options.MinPoints)
      return;

    // ✅ 防重入：如果正在跑，就跳過（下一次取樣再檢測）
    if (Interlocked.CompareExchange(ref _detectGate, 1, 0) != 0)
      return;

    // snapshot：避免背景計算時 buffer 被修改
    var snapshot = _buffer.ToArray();

    _ = Task.Run(() => Detect(snapshot, nowMs))
        .ContinueWith(_ => Interlocked.Exchange(ref _detectGate, 0), TaskScheduler.Default);
  }

  private void TrimOld(long nowMs) {
    var minTs = nowMs - _options.WindowMs;

    int removeCount = 0;
    for (int i = 0; i < _buffer.Count; i++) {
      if (_buffer[i].TimestampMs < minTs) removeCount++;
      else break;
    }

    if (removeCount > 0)
      _buffer.RemoveRange(0, removeCount);

    if (_buffer.Count > 128)
      _buffer.RemoveRange(0, _buffer.Count - 128);
  }

  private void Detect(TimedPoint[] snapshot, long nowMs) {
    var sw = Stopwatch.StartNew();
    var result = _detector.Detect(snapshot, _options, out var dbg);
    sw.Stop();

    // 統計（不刷 log）
    Interlocked.Increment(ref _checks);
    Interlocked.Add(ref _totalCostMs, sw.ElapsedMilliseconds);
    _lastDebug = dbg;

    if (result == GestureResult.Circle) {
      Interlocked.Increment(ref _hits);

      _lastTriggerMs = nowMs;

      // 觸發後清 buffer，避免連續誤觸
      // 注意：這裡在背景 thread，List 不是 thread-safe
      // ✅ 做法：回到 UI thread 清空比較安全（但清空很快，且這裡只在 hit 才做）
      // 我用最保守：請求 UI thread 清空
      System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
      {
        _buffer.Clear();
        _lastX = double.NaN;
        _lastY = double.NaN;
      });

      _logger.LogInformation(
          "Gesture HIT circle points={Points} cover={Cover:F2} r={R:F1} stdRatio={Std:F2} costMs={Cost}",
          dbg.PointCount, dbg.AngleCoverage, dbg.MeanRadius, dbg.RadiusStdDevRatio, sw.ElapsedMilliseconds);

      // ✅ 語意乾淨：明確指出來源是 Gesture
      _input.RequestMenu(MenuRequestSource.Gesture);
    }

    // 每 5 秒彙總一次
    if (_statSw.ElapsedMilliseconds >= 5000) {
      var checks = Interlocked.Exchange(ref _checks, 0);
      var hits = Interlocked.Exchange(ref _hits, 0);
      var cost = Interlocked.Exchange(ref _totalCostMs, 0);
      var avg = checks > 0 ? (double)cost / checks : 0;

      _statSw.Restart();

      _logger.LogInformation(
          "Gesture stats checks={Checks} hits={Hits} avgCostMs={AvgCost:F2} last(points={P} cover={Cover:F2} r={R:F1} std={Std:F2})",
          checks, hits, avg,
          _lastDebug.PointCount, _lastDebug.AngleCoverage, _lastDebug.MeanRadius, _lastDebug.RadiusStdDevRatio);
    }
  }
}
