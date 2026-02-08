using OrbitBubble.Core.Common;
using OrbitBubble.Domain.Bubbles;

namespace OrbitBubble.Domain.Launch;

public interface ILauncherService {
  Result<bool> Launch(BubbleItem item);
}
