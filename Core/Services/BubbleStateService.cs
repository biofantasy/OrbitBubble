using OrbitBubble.Core.Models;

namespace OrbitBubble.Core.Services;

public class BubbleStateService {
  private readonly Stack<List<BubbleItem>> _navHistory = new();

  public List<BubbleItem> AllBubbles { get; private set; } = new();
  public List<BubbleItem> CurrentViewBubbles { get; private set; } = new();
  public bool IsAtRoot => _navHistory.Count == 0;

  public void Initialize(List<BubbleItem> allBubbles) {
    AllBubbles = allBubbles ?? new List<BubbleItem>();
    CurrentViewBubbles = new List<BubbleItem>(AllBubbles);
    _navHistory.Clear();
  }

  public void ResetToRoot() {
    _navHistory.Clear();
    CurrentViewBubbles = new List<BubbleItem>(AllBubbles);
  }

  public void ExpandCollection(BubbleItem collection) {
    _navHistory.Push(new List<BubbleItem>(CurrentViewBubbles));
    CurrentViewBubbles = collection.SubItems;
  }

  public bool TryBackToParent() {
    if (_navHistory.Count == 0) {
      return false;
    }

    CurrentViewBubbles = _navHistory.Pop();
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

    // 在根層檢視時，新增項目應立即納入目前畫面資料源
    if (IsAtRoot && added.Count > 0) {
      CurrentViewBubbles.AddRange(added);
    }

    return added;
  }
}
