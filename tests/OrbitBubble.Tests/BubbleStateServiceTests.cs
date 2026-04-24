using OrbitBubble.Core.Models;
using OrbitBubble.Core.Services;

namespace OrbitBubble.Tests;

public class BubbleStateServiceTests {
  [Fact]
  public void CurrentNavigatedCollection_AfterBackFromNested_ReflectsRemainingStack() {
    var inner = new BubbleItem {
      Name = "inner",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem>()
    };
    var outer = new BubbleItem {
      Name = "outer",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem> { inner }
    };
    var service = new BubbleStateService();
    service.Initialize(new List<BubbleItem> { outer });

    service.ExpandCollection(outer);
    Assert.Equal("outer", service.CurrentNavigatedCollection?.Name);

    service.ExpandCollection(inner);
    Assert.Equal("inner", service.CurrentNavigatedCollection?.Name);

    Assert.True(service.TryBackToParent());
    Assert.False(service.IsAtRoot);
    Assert.Equal("outer", service.CurrentNavigatedCollection?.Name);

    Assert.True(service.TryBackToParent());
    Assert.True(service.IsAtRoot);
    Assert.Null(service.CurrentNavigatedCollection);
  }

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
  public void AddFiles_WhenInsideCollection_AddsToCurrentViewNotRoot() {
    var service = new BubbleStateService();
    var collection = new BubbleItem {
      Name = "folder",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem>()
    };
    service.Initialize(new List<BubbleItem> { collection });
    service.ExpandCollection(collection);

    var added = service.AddFiles(new[] { "C:\\tmp\\nested.txt" });

    Assert.Single(added);
    Assert.Single(service.AllBubbles);
    Assert.Equal(collection, service.AllBubbles[0]);
    Assert.Single(collection.SubItems);
    Assert.Equal("nested.txt", collection.SubItems[0].Name);
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
  public void ApplyMerge_AtRoot_AddsMergedItemOnlyOnce() {
    var a = new BubbleItem { Name = "a", Path = "C:\\tmp\\a.txt" };
    var b = new BubbleItem { Name = "b", Path = "C:\\tmp\\b.txt" };
    var service = new BubbleStateService();
    service.Initialize(new List<BubbleItem> { a, b });

    var merged = new BubbleItem {
      Name = "Merge",
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem> { a, b }
    };
    service.ApplyMerge(a, b, merged);

    Assert.Single(service.AllBubbles);
    Assert.Same(merged, service.AllBubbles[0]);
    Assert.Single(service.CurrentViewBubbles);
    Assert.Same(merged, service.CurrentViewBubbles[0]);
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
