namespace OrbitBubble.Domain.Bubbles;

public sealed class BubbleState {
  public BubbleRoot Root { get; }

  // 目前顯示的容器：null 表示 Root.Items
  public BubbleItem? CurrentCollection { get; private set; }

  // 導航歷史：存上一層 collection
  private readonly Stack<BubbleItem?> _history = new();

  public BubbleState(BubbleRoot root) {
    Root = root;
  }

  public IReadOnlyList<BubbleItem> GetVisibleItems()
      => CurrentCollection?.Children ?? Root.Items;

  public void EnterCollection(BubbleItem collection) {
    _history.Push(CurrentCollection);
    CurrentCollection = collection;
  }

  public bool CanBack => _history.Count > 0;

  public void Back() {
    if (_history.Count > 0)
      CurrentCollection = _history.Pop();
  }

  public void ResetToRoot() {
    _history.Clear();
    CurrentCollection = null;
  }
}
