using OrbitBubble.Core.Models;
using OrbitBubble.Core.Services;

namespace OrbitBubble.Tests;

public class BubbleInteractionServiceTests {
  [Fact]
  public void CreateMergedCollection_SetsCollectionMetadataAndCombinesItems() {
    var service = new BubbleInteractionService();
    var a = new BubbleItem { Name = "a", Path = "C:\\tmp\\a.txt" };
    var b = new BubbleItem { Name = "b", Path = "C:\\tmp\\b.txt" };

    var merged = service.CreateMergedCollection(a, b);

    Assert.Equal(BubbleConstants.MergedBubbleName, merged.Name);
    Assert.Equal(BubbleConstants.CollectionPath, merged.Path);
    Assert.Equal(2, merged.SubItems.Count);
    Assert.Contains(a, merged.SubItems);
    Assert.Contains(b, merged.SubItems);
  }
}
