using OrbitBubble.Core.Models;

namespace OrbitBubble.Core.Services;

public class BubbleStateService {
  private readonly Stack<BubbleItem> _collectionStack = new();

  public List<BubbleItem> AllBubbles { get; private set; } = new();
  public List<BubbleItem> CurrentViewBubbles { get; private set; } = new();
  public bool IsAtRoot => _collectionStack.Count == 0;

  /// <summary>
  /// 非根層時為導航堆疊頂端（目前畫面所屬集合）；根層為 null。供 CenterHub 顯示名稱用。
  /// </summary>
  public BubbleItem? CurrentNavigatedCollection =>
    _collectionStack.Count > 0 ? _collectionStack.Peek() : null;

  public void Initialize(List<BubbleItem> allBubbles) {
    AllBubbles = allBubbles ?? new List<BubbleItem>();
    CurrentViewBubbles = AllBubbles;
    _collectionStack.Clear();
  }

  public void ResetToRoot() {
    _collectionStack.Clear();
    CurrentViewBubbles = AllBubbles;
  }

  public void ExpandCollection(BubbleItem collection) {
    _collectionStack.Push(collection);
    CurrentViewBubbles = collection.SubItems;
  }

  public bool TryBackToParent() {
    if (_collectionStack.Count == 0) {
      return false;
    }

    _collectionStack.Pop();
    CurrentViewBubbles = _collectionStack.Count > 0
      ? _collectionStack.Peek().SubItems
      : AllBubbles;
    return true;
  }

  public void ApplyMerge(BubbleItem targetData, BubbleItem sourceData, BubbleItem mergedData) {
    CurrentViewBubbles.Remove(targetData);
    CurrentViewBubbles.Remove(sourceData);
    CurrentViewBubbles.Add(mergedData);
  }

  public bool MoveCurrentBubbleToParent(BubbleItem item) {
    if (IsAtRoot) return false;
    if (!CurrentViewBubbles.Remove(item)) return false;

    if (_collectionStack.Count == 1) {
      AllBubbles.Add(item);
      return true;
    }

    var parentCollection = _collectionStack.ToArray()[1];
    parentCollection.SubItems.Add(item);
    return true;
  }

  public bool ReorderInCurrentView(BubbleItem item, int targetIndex) {
    int oldIndex = CurrentViewBubbles.IndexOf(item);
    if (oldIndex < 0) return false;

    int clampedTarget = Math.Clamp(targetIndex, 0, CurrentViewBubbles.Count - 1);
    if (oldIndex == clampedTarget) return false;

    CurrentViewBubbles.RemoveAt(oldIndex);
    if (clampedTarget > oldIndex) {
      clampedTarget--;
    }
    CurrentViewBubbles.Insert(clampedTarget, item);
    return true;
  }

  public bool RemoveBubble(BubbleItem item) {
    bool removedFromAll = AllBubbles.Remove(item);
    bool removedFromCurrent = CurrentViewBubbles.Remove(item);
    return removedFromAll || removedFromCurrent;
  }

  /// <summary>
  /// 拖放檔案／資料夾時：根層加入 AllBubbles；在集合內則加入目前層級的 SubItems，避免誤加到根層。
  /// </summary>
  public List<BubbleItem> AddFiles(IEnumerable<string> filePaths) {
    var added = new List<BubbleItem>();
    foreach (var path in filePaths) {
      if (ContainsPathInTree(AllBubbles, path)) {
        continue;
      }

      var newItem = new BubbleItem {
        Name = System.IO.Path.GetFileName(path),
        Path = path
      };

      if (IsAtRoot) {
        AllBubbles.Add(newItem);
      } else {
        CurrentViewBubbles.Add(newItem);
      }

      added.Add(newItem);
    }

    return added;
  }

  /// <summary>
  /// 遞迴檢查路徑是否已存在於樹中（含巢狀集合），供拖放去重用。
  /// </summary>
  private static bool ContainsPathInTree(IEnumerable<BubbleItem> items, string path) {
    foreach (var b in items) {
      if (b.Path == path) {
        return true;
      }

      if (ContainsPathInTree(b.SubItems, path)) {
        return true;
      }
    }

    return false;
  }
}
