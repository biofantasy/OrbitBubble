using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Models;
using System.Collections.Concurrent;
using System.Windows.Media;

namespace OrbitBubble.Core.Services;

public class IconCacheService {
  private readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);
  private readonly string _fallbackFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

  /// <summary>
  /// 集中處理泡泡圖示來源與快取，避免重複取用系統圖示造成卡頓。
  /// </summary>
  public ImageSource? GetIcon(BubbleItem item) {
    string iconPath = ResolveIconPath(item);
    return _iconCache.GetOrAdd(iconPath, key => IconHelper.GetIcon(key));
  }

  private string ResolveIconPath(BubbleItem item) {
    if (item.SubItems.Count > 0 || item.Path == BubbleConstants.CollectionPath) {
      return _fallbackFolderPath;
    }

    return string.IsNullOrWhiteSpace(item.Path) ? _fallbackFolderPath : item.Path;
  }
}
