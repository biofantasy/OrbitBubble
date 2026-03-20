using OrbitBubble.Core.Models;

namespace OrbitBubble.Core.Services;

public class BubbleStateService {
  private readonly Stack<BubbleItem> _collectionStack = new();

  public List<BubbleItem> AllBubbles { get; private set; } = new();
  public List<BubbleItem> CurrentViewBubbles { get; private set; } = new();
  public bool IsAtRoot => _collectionStack.Count == 0;

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

    // 根層合併時同步更新完整資料來源
    if (IsAtRoot) {
      AllBubbles.Remove(targetData);
      AllBubbles.Remove(sourceData);
      AllBubbles.Add(mergedData);
    }
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

  public List<BubbleItem> AddFiles(IEnumerable<string> filePaths) {
    var added = new List<BubbleItem>();
    foreach (var path in filePaths) {
      if (AllBubbles.Any(b => b.Path == path)) {
        continue;
      }

      var newItem = new BubbleItem {
        Name = System.IO.Path.GetFileName(path),
        Path = path
      };
      AllBubbles.Add(newItem);
      added.Add(newItem);
    }

    // 在根層且目前檢視不是同一個清單引用時，才補到目前畫面清單
    if (IsAtRoot && added.Count > 0 && !ReferenceEquals(CurrentViewBubbles, AllBubbles)) {
      CurrentViewBubbles.AddRange(added);
    }

    return added;
  }
}
