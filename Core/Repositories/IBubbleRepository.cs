using OrbitBubble.Core.Models;

namespace OrbitBubble.Core.Repositories;

public interface IBubbleRepository {
  List<BubbleItem> LoadAll();
  void SaveAll(List<BubbleItem> bubbles);
}
