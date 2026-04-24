using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Models;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace OrbitBubble.Core.Services;

public class IconCacheService {
  private readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
  private readonly string _fallbackFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

  public ImageSource? GetIcon(BubbleItem item) {
    string iconPath = ResolveIconPath(item);
    return _iconCache.GetOrAdd(iconPath, key => IconHelper.GetIcon(key));
  }

  /// <summary>
  /// 非同步取得圖示，避免在 UI thread 阻塞。快取命中時直接回傳，miss 時在背景載入。
  /// </summary>
  public async Task<ImageSource?> GetIconAsync(BubbleItem item) {
    string iconPath = ResolveIconPath(item);

    if (_iconCache.TryGetValue(iconPath, out var cached))
      return cached;

    var icon = await Task.Run(() => IconHelper.GetIcon(iconPath)).ConfigureAwait(false);
    _iconCache[iconPath] = icon;
    return icon;
  }

  /// <summary>
  /// 預熱指定 items 的圖示快取，建議在程式啟動時呼叫。
  /// </summary>
  public Task PrewarmAsync(IEnumerable<BubbleItem> items) {
    var paths = items
        .Select(ResolveIconPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(p => !_iconCache.ContainsKey(p))
        .ToList();

    return Task.WhenAll(paths.Select(p => Task.Run(() => {
      var icon = IconHelper.GetIcon(p);
      _iconCache[p] = icon;
    })));
  }

  public string ResolveIconPath(BubbleItem item) {
    if (item.SubItems.Count > 0 || item.Path == BubbleConstants.CollectionPath)
      return _fallbackFolderPath;

    return string.IsNullOrWhiteSpace(item.Path) ? _fallbackFolderPath : item.Path;
  }
}
