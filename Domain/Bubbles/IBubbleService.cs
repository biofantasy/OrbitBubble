using OrbitBubble.Core.Common;

namespace OrbitBubble.Domain.Bubbles;

public interface IBubbleService {
  BubbleState State { get; }

  Result<BubbleState> Load();

  Result<BubbleItem> AddFile(string path, string? displayName = null);
  Result<BubbleItem> AddFolder(string path, string? displayName = null);

  Result<bool> Remove(string id);

  Result<BubbleItem> MergeIntoCollection(string sourceId, string targetId);

  Result<bool> EnterCollection(string id);
  Result<bool> Back();

  Result<bool> SaveNow();         // 手動立即存
  void RequestSave();             // 延遲存（debounce）
}
