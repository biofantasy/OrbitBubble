using OrbitBubble.Core.Models;
using System.IO;

namespace OrbitBubble.Core.Services;

public class BubbleValidationService {
  public bool IsAvailable(BubbleItem item) {
    if (item.SubItems.Count > 0 || item.Path == BubbleConstants.CollectionPath) {
      return true;
    }

    return IsLeafPathAvailable(item.Path);
  }

  public int RemoveInvalidLinks(List<BubbleItem> rootItems) {
    return RemoveInvalidLinksRecursive(rootItems);
  }

  private int RemoveInvalidLinksRecursive(List<BubbleItem> items) {
    int removedCount = 0;

    for (int i = items.Count - 1; i >= 0; i--) {
      var item = items[i];
      if (item.SubItems.Count > 0 || item.Path == BubbleConstants.CollectionPath) {
        removedCount += RemoveInvalidLinksRecursive(item.SubItems);
        continue;
      }

      if (!IsLeafPathAvailable(item.Path)) {
        items.RemoveAt(i);
        removedCount++;
      }
    }

    return removedCount;
  }

  private static bool IsLeafPathAvailable(string? path) {
    if (string.IsNullOrWhiteSpace(path)) return false;
    return File.Exists(path) || Directory.Exists(path);
  }
}
