using System;
using System.Collections.Generic;
using System.Text;

namespace OrbitBubble.Domain.Bubbles;

public enum BubbleItemType {
  File,
  Folder,
  Collection,
  Command
}

public sealed class BubbleItem {
  public string Id { get; set; } = Guid.NewGuid().ToString("N");
  public string DisplayName { get; set; } = "";
  public BubbleItemType Type { get; set; }

  // File/Folder 常用
  public string? Path { get; set; }

  // Collection 用
  public List<BubbleItem>? Children { get; set; }

  // 可擴充：最後使用、排序等
  public Dictionary<string, string>? Metadata { get; set; }
}
