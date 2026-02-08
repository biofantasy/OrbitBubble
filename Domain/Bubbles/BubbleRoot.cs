namespace OrbitBubble.Domain.Bubbles;

public sealed class BubbleRoot {
  public int Version { get; set; } = 1;
  public List<BubbleItem> Items { get; set; } = new();
}
