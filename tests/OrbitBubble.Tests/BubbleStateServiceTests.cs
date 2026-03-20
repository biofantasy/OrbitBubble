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

  [Fact]
  public void MoveCurrentBubbleToParent_WhenInCollection_MovesItemToUpperLevel() {
    var service = new BubbleStateService();
    var inner = new BubbleItem { Name = "inner", Path = "C:\\tmp\\inner.txt" };
    var collection = new BubbleItem {
      Name = "merge",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem> { inner }
    };

    service.Initialize(new List<BubbleItem> { collection });
    service.ExpandCollection(collection);

    var moved = service.MoveCurrentBubbleToParent(inner);

    Assert.True(moved);
    Assert.Empty(collection.SubItems);
    Assert.Equal(2, service.AllBubbles.Count);
    Assert.Contains(service.AllBubbles, x => x.Name == "inner");
  }

  [Fact]
  public void ReorderInCurrentView_MovesItemToTargetIndex() {
    var service = new BubbleStateService();
    var a = new BubbleItem { Name = "1", Path = "C:\\tmp\\1.txt" };
    var b = new BubbleItem { Name = "2", Path = "C:\\tmp\\2.txt" };
    var c = new BubbleItem { Name = "3", Path = "C:\\tmp\\3.txt" };
    service.Initialize(new List<BubbleItem> { a, b, c });

    var changed = service.ReorderInCurrentView(a, 2);

    Assert.True(changed);
    Assert.Equal("2", service.CurrentViewBubbles[0].Name);
    Assert.Equal("1", service.CurrentViewBubbles[1].Name);
    Assert.Equal("3", service.CurrentViewBubbles[2].Name);
  }
}
