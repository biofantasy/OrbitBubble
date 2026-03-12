using OrbitBubble.Core.Models;
using OrbitBubble.Core.Services;

namespace OrbitBubble.Tests;

public class BubbleValidationServiceTests {
  [Fact]
  public void IsAvailable_ReturnsFalse_WhenLeafPathMissing() {
    var service = new BubbleValidationService();
    var item = new BubbleItem { Name = "missing", Path = "Z:\\__path_not_exists__\\x.txt" };

    var ok = service.IsAvailable(item);

    Assert.False(ok);
  }

  [Fact]
  public void RemoveInvalidLinks_RemovesOnlyInvalidLeafItems() {
    var service = new BubbleValidationService();
    var validFile = Path.GetTempFileName();
    try {
      var root = new List<BubbleItem> {
        new BubbleItem { Name = "valid", Path = validFile },
        new BubbleItem { Name = "invalid", Path = "Z:\\__path_not_exists__\\bad.txt" },
        new BubbleItem {
          Name = "group",
          Path = BubbleConstants.CollectionPath,
          SubItems = new List<BubbleItem> {
            new BubbleItem { Name = "nested-invalid", Path = "Z:\\__path_not_exists__\\nested.txt" }
          }
        }
      };

      var removed = service.RemoveInvalidLinks(root);

      Assert.Equal(2, removed);
      Assert.Equal(2, root.Count);
      Assert.Contains(root, x => x.Name == "valid");
      Assert.Contains(root, x => x.Name == "group");
    } finally {
      if (File.Exists(validFile)) File.Delete(validFile);
    }
  }
}
