using System;
using System.Collections.Generic;
using System.Text;

namespace OrbitBubble.Core.Models;

public class BubbleItem {
  public string Name { get; set; } = "";
  public string? Path { get; set; }
  public bool IsCollection => SubItems.Count > 0;
  public List<BubbleItem> SubItems { get; set; } = new(); // 如果這是集合，裡面會有內容
}
