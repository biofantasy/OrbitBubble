using OrbitBubble.Core.Common;
using OrbitBubble.Domain.Bubbles;

namespace OrbitBubble.Core.Stores;

public interface IBubbleStore {
  Result<BubbleRoot> Load();
  Result<bool> Save(BubbleRoot root); // bool 代表是否成功寫入（你也可以用 Unit）
}
