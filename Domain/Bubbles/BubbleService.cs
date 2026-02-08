using System.IO;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Common;
using OrbitBubble.Core.Stores;

namespace OrbitBubble.Domain.Bubbles;

public sealed class BubbleService : IBubbleService, IDisposable {
  private readonly ILogger<BubbleService> _logger;
  private readonly IBubbleStore _store;

  private Timer? _saveTimer;
  private readonly object _saveLock = new();
  private volatile bool _dirty;

  public BubbleState State { get; private set; } = new(new BubbleRoot());

  public BubbleService(ILogger<BubbleService> logger, IBubbleStore store) {
    _logger = logger;
    _store = store;
  }

  public Result<BubbleState> Load() {
    var load = _store.Load();
    if (!load.IsSuccess)
      return Result<BubbleState>.Fail(load.Error!);

    State = new BubbleState(load.Value!);

    _logger.LogInformation("BubbleService loaded. items={Count}", State.Root.Items.Count);
    return Result<BubbleState>.Ok(State);
  }

  public Result<BubbleItem> AddFile(string path, string? displayName = null)
      => AddPath(path, BubbleItemType.File, displayName);

  public Result<BubbleItem> AddFolder(string path, string? displayName = null)
      => AddPath(path, BubbleItemType.Folder, displayName);

  private Result<BubbleItem> AddPath(string path, BubbleItemType type, string? displayName) {
    if (string.IsNullOrWhiteSpace(path))
      return Result<BubbleItem>.Fail(new StoreError("BUBBLE_PATH_EMPTY", "Path is empty."));

    // 可選：存在性檢查（你若允許不存在的 shortcut/command，就不要檢查）
    if (type == BubbleItemType.File && !File.Exists(path))
      return Result<BubbleItem>.Fail(new StoreError("BUBBLE_FILE_NOT_FOUND", $"File not found: {path}"));
    if (type == BubbleItemType.Folder && !Directory.Exists(path))
      return Result<BubbleItem>.Fail(new StoreError("BUBBLE_DIR_NOT_FOUND", $"Folder not found: {path}"));

    var list = GetMutableCurrentList();

    // 去重策略：同一層內同 path + type 不重複
    var dup = list.FirstOrDefault(x =>
        x.Type == type &&
        string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

    if (dup != null)
      return Result<BubbleItem>.Ok(dup);

    var item = new BubbleItem {
      Id = Guid.NewGuid().ToString("N"),
      DisplayName = displayName ?? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)),
      Type = type,
      Path = path
    };

    list.Add(item);

    _logger.LogInformation("Bubble added. type={Type} id={Id} path={Path}", item.Type, item.Id, item.Path);

    MarkDirtyAndRequestSave();
    return Result<BubbleItem>.Ok(item);
  }

  public Result<bool> Remove(string id) {
    if (string.IsNullOrWhiteSpace(id))
      return Result<bool>.Fail(new StoreError("BUBBLE_ID_EMPTY", "Id is empty."));

    var list = GetMutableCurrentList();
    var idx = list.FindIndex(x => x.Id == id);
    if (idx < 0)
      return Result<bool>.Fail(new StoreError("BUBBLE_NOT_FOUND", $"Bubble not found: {id}"));

    var removed = list[idx];
    list.RemoveAt(idx);

    _logger.LogInformation("Bubble removed. id={Id} type={Type} path={Path}", removed.Id, removed.Type, removed.Path);

    MarkDirtyAndRequestSave();
    return Result<bool>.Ok(true);
  }

  /// <summary>
  /// Merge 規則：
  /// - source 拖到 target 上
  /// - 若 target 是 Collection：source 加進 target.Children
  /// - 若 target 不是 Collection：建立新的 Collection，包含 target + source，並用新 collection 取代 target 的位置
  /// - source 從原本位置移除
  /// </summary>
  public Result<BubbleItem> MergeIntoCollection(string sourceId, string targetId) {
    if (sourceId == targetId)
      return Result<BubbleItem>.Fail(new StoreError("MERGE_SAME_ID", "Cannot merge the same bubble."));

    var list = GetMutableCurrentList();

    var source = list.FirstOrDefault(x => x.Id == sourceId);
    var target = list.FirstOrDefault(x => x.Id == targetId);

    if (source == null || target == null)
      return Result<BubbleItem>.Fail(new StoreError("MERGE_NOT_FOUND", "Source or target not found in current level."));

    // 先把 source 從同層移除
    list.Remove(source);

    BubbleItem collection;

    if (target.Type == BubbleItemType.Collection) {
      target.Children ??= new();
      target.Children.Add(source);

      collection = target;

      _logger.LogInformation("Merged into existing collection. collectionId={Id} addedId={AddedId}", collection.Id, source.Id);
    } else {
      // 建新 collection，位置用 target 原本 index
      var targetIndex = list.FindIndex(x => x.Id == target.Id);

      // target 還在 list 裡，先移除 target
      list.Remove(target);

      collection = new BubbleItem {
        Id = Guid.NewGuid().ToString("N"),
        DisplayName = $"{target.DisplayName} + {source.DisplayName}",
        Type = BubbleItemType.Collection,
        Children = new() { target, source }
      };

      // 插回原本 target 的位置（如果找不到就 append）
      if (targetIndex >= 0 && targetIndex <= list.Count)
        list.Insert(targetIndex, collection);
      else
        list.Add(collection);

      _logger.LogInformation("Created new collection via merge. newCollectionId={Id} childA={A} childB={B}",
          collection.Id, target.Id, source.Id);
    }

    MarkDirtyAndRequestSave();
    return Result<BubbleItem>.Ok(collection);
  }

  public Result<bool> EnterCollection(string id) {
    var current = State.GetVisibleItems().FirstOrDefault(x => x.Id == id);
    if (current == null)
      return Result<bool>.Fail(new StoreError("NAV_NOT_FOUND", $"Target not found: {id}"));

    if (current.Type != BubbleItemType.Collection)
      return Result<bool>.Fail(new StoreError("NAV_NOT_COLLECTION", "Target is not a collection."));

    State.EnterCollection(current);
    _logger.LogInformation("Enter collection. id={Id} name={Name}", current.Id, current.DisplayName);
    return Result<bool>.Ok(true);
  }

  public Result<bool> Back() {
    if (!State.CanBack)
      return Result<bool>.Ok(false);

    State.Back();
    _logger.LogInformation("Navigate back.");
    return Result<bool>.Ok(true);
  }

  public Result<bool> SaveNow() {
    lock (_saveLock) {
      _saveTimer?.Dispose();
      _saveTimer = null;
    }

    var save = _store.Save(State.Root);
    if (!save.IsSuccess)
      return Result<bool>.Fail(save.Error!);

    _dirty = false;
    return Result<bool>.Ok(true);
  }

  public void RequestSave() {
    MarkDirtyAndRequestSave();
  }

  private void MarkDirtyAndRequestSave() {
    _dirty = true;

    lock (_saveLock) {
      // ✅ debounce：500ms 內連續多次操作，只存一次
      _saveTimer?.Dispose();
      _saveTimer = new Timer(_ =>
      {
        try {
          if (!_dirty) return;

          var save = _store.Save(State.Root);
          if (!save.IsSuccess) {
            _logger.LogError(save.Error!.Exception,
                "Auto-save failed. code={Code} msg={Msg}",
                save.Error.Code, save.Error.Message);
            return;
          }

          _dirty = false;
          _logger.LogInformation("Auto-save succeeded.");
        } catch (Exception ex) {
          _logger.LogError(ex, "Auto-save threw exception.");
        }
      }, null, dueTime: 500, period: Timeout.Infinite);
    }
  }

  private List<BubbleItem> GetMutableCurrentList() {
    if (State.CurrentCollection == null)
      return State.Root.Items;

    State.CurrentCollection.Children ??= new();
    return State.CurrentCollection.Children;
  }

  public void Dispose() {
    lock (_saveLock) {
      _saveTimer?.Dispose();
      _saveTimer = null;
    }
  }
}
