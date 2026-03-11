using OrbitBubble.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace OrbitBubble.Core.Services;

public class BubbleInteractionService {
  /// <summary>
  /// 依滑鼠移動更新元素座標，統一處理 NaN 初始值。
  /// </summary>
  public void MoveElement(FrameworkElement element, Point fromPos, Point toPos) {
    double deltaX = toPos.X - fromPos.X;
    double deltaY = toPos.Y - fromPos.Y;

    double left = Canvas.GetLeft(element);
    double top = Canvas.GetTop(element);
    if (double.IsNaN(left)) left = 0;
    if (double.IsNaN(top)) top = 0;

    Canvas.SetLeft(element, left + deltaX);
    Canvas.SetTop(element, top + deltaY);
  }

  /// <summary>
  /// 計算兩個元素中心點距離。
  /// </summary>
  public double GetDistance(UIElement e1, UIElement e2) {
    if (e1 is not FrameworkElement fe1 || e2 is not FrameworkElement fe2) {
      return double.MaxValue;
    }

    double x1 = Canvas.GetLeft(e1) + fe1.ActualWidth / 2;
    double y1 = Canvas.GetTop(e1) + fe1.ActualHeight / 2;
    double x2 = Canvas.GetLeft(e2) + fe2.ActualWidth / 2;
    double y2 = Canvas.GetTop(e2) + fe2.ActualHeight / 2;

    return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
  }

  /// <summary>
  /// 從目前元素集合中找出可合併目標。
  /// </summary>
  public FrameworkElement? FindMergeTarget(
    IEnumerable<FrameworkElement> allItems,
    UIElement draggedBubble,
    UIElement centerHub,
    double mergeDistanceThreshold) {
    foreach (var other in allItems) {
      if (other == draggedBubble || other == centerHub) continue;
      if (other.Tag is not BubbleItem) continue;

      if (GetDistance(draggedBubble, other) < mergeDistanceThreshold) {
        return other;
      }
    }

    return null;
  }

  /// <summary>
  /// 建立合併後的集合泡泡資料。
  /// </summary>
  public BubbleItem CreateMergedCollection(BubbleItem targetData, BubbleItem sourceData) {
    var collectionData = new BubbleItem {
      Name = BubbleConstants.MergedBubbleName,
      Path = BubbleConstants.CollectionPath,
      SubItems = new List<BubbleItem>()
    };

    if (targetData.SubItems.Count > 0) {
      collectionData.SubItems.AddRange(targetData.SubItems);
    } else {
      collectionData.SubItems.Add(targetData);
    }

    if (sourceData.SubItems.Count > 0) {
      collectionData.SubItems.AddRange(sourceData.SubItems);
    } else {
      collectionData.SubItems.Add(sourceData);
    }

    return collectionData;
  }
}
