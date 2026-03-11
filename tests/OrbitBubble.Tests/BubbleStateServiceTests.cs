using OrbitBubble.Core.Models;
using OrbitBubble.Core.Services;

namespace OrbitBubble.Tests;

public class BubbleStateServiceTests {
  [Fact]
  public void ExpandAndBackToParent_RestoresPreviousView() {
    var service = new BubbleStateService();
    var child = new BubbleItem { Name = "child", Path = "C:\\tmp\\child.txt" };
    var collection = new BubbleItem { Name = "folder", Path = BubbleConstants.CollectionPath, SubItems = new List<BubbleItem> { child } };
    var rootItem = new BubbleItem { Name = "rootA", Path = "C:\\tmp\\a.txt" };
    service.Initialize(new List<BubbleItem> { rootItem, collection });

    service.ExpandCollection(collection);
    Assert.False(service.IsAtRoot);
    Assert.Single(service.CurrentViewBubbles);
    Assert.Equal("child", service.CurrentViewBubbles[0].Name);

    var backOk = service.TryBackToParent();
    Assert.True(backOk);
    Assert.True(service.IsAtRoot);
    Assert.Equal(2, service.CurrentViewBubbles.Count);
  }

  [Fact]
  public void AddFiles_DeduplicatesAndUpdatesRootView() {
    var service = new BubbleStateService();
    service.Initialize(new List<BubbleItem> {
      new BubbleItem { Name = "a.txt", Path = "C:\\tmp\\a.txt" }
    });

    var added = service.AddFiles(new[] { "C:\\tmp\\a.txt", "C:\\tmp\\b.txt", "C:\\tmp\\c.txt" });

    Assert.Equal(2, added.Count);
    Assert.Equal(3, service.AllBubbles.Count);
    Assert.Equal(3, service.CurrentViewBubbles.Count);
  }
}
