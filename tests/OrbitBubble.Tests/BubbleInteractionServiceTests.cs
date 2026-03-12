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

  [Fact]
  public void CreateMergedCollection_PreservesRenamedCollectionName() {
    var service = new BubbleInteractionService();
    var renamedCollection = new BubbleItem {
      Name = "工作資料夾",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem> {
        new BubbleItem { Name = "old", Path = "C:\\tmp\\old.txt" }
      }
    };
    var newFile = new BubbleItem { Name = "new", Path = "C:\\tmp\\new.txt" };

    var merged = service.CreateMergedCollection(renamedCollection, newFile);

    Assert.Equal("工作資料夾", merged.Name);
    Assert.Equal(BubbleConstants.CollectionPath, merged.Path);
    Assert.Equal(2, merged.SubItems.Count);
  }
}
