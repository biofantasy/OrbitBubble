using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Helpers;

namespace OrbitBubble.Core.Icons;

public sealed class ShellIconProvider : IIconProvider {

  private readonly ILogger<ShellIconProvider> _logger;

  private readonly SemaphoreSlim _gate = new(initialCount: 6, maxCount: 6);

  // cache：同一個 path 不重複抓
  private readonly ConcurrentDictionary<string, Lazy<Task<ImageSource>>> _cache = new(StringComparer.OrdinalIgnoreCase);

  public ImageSource Placeholder { get; }

  public ShellIconProvider(ILogger<ShellIconProvider> logger) {
    _logger = logger;
    // 你可以換成你自己的資源圖
    Placeholder = CreateDefaultPlaceholder();
  }

  public Task<ImageSource> GetIconAsync(string path, CancellationToken ct = default) {
    if (string.IsNullOrWhiteSpace(path))
      return Task.FromResult(Placeholder);

    // Lazy<Task<T>> 避免同時重複載入同一路徑
    var lazy = _cache.GetOrAdd(path, p => new Lazy<Task<ImageSource>>(() => LoadIconInternalAsync(p, ct)));
    return lazy.Value;
  }

  private async Task<ImageSource> LoadIconInternalAsync(string path, CancellationToken ct) {
    var sw = Stopwatch.StartNew();
    await _gate.WaitAsync(ct).ConfigureAwait(false);
    try {
      // 背景執行：呼叫你現有同步 IconHelper（或你要改成內建 SHGetFileInfo 也行）
      var icon = await Task.Run(() =>
      {
        try {
          // 這裡請改成你現有 IconHelper 的方法
          // var img = IconHelper.GetIcon(path);
          // return img ?? Placeholder;

          var img = IconHelper.GetIcon(path); // <-- 你專案現有
          return img ?? Placeholder;
        } catch (Exception ex) {
          _logger.LogWarning(ex, "Icon load failed. path={Path}", path);
          return Placeholder;
        }
      }, ct).ConfigureAwait(false);

      sw.Stop();
      _logger.LogDebug("Icon loaded. path={Path} costMs={Cost}", path, sw.ElapsedMilliseconds);

      // ImageSource 若需要 freeze（跨 thread 安全），可以嘗試 Freeze
      if (icon.CanFreeze) icon.Freeze();

      return icon;
    } finally {
      _gate.Release();
    }
  }

  private static ImageSource CreateDefaultPlaceholder() {
    // 最簡 placeholder：透明空圖，避免 null
    // 你也可以改用 pack://application:,,,/Resources/placeholder.png
    var drawing = new System.Windows.Media.DrawingImage();
    drawing.Freeze();
    return drawing;
  }
}

