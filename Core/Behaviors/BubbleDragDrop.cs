using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using OrbitBubble.Core.ViewModels;
using OrbitBubble.Domain;
using OrbitBubble.Domain.Bubbles;

namespace OrbitBubble.Core.Behaviors;

public static class BubbleDragDrop {
  // 附加到 Bubble Item Root（例如 Border）上，開啟 drag/drop
  public static readonly DependencyProperty EnableProperty =
      DependencyProperty.RegisterAttached(
          "Enable",
          typeof(bool),
          typeof(BubbleDragDrop),
          new PropertyMetadata(false, OnEnableChanged));

  public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
  public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

  private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is not FrameworkElement fe) return;

    if ((bool)e.NewValue) {
      fe.PreviewMouseMove += OnPreviewMouseMove;
      fe.AllowDrop = true;
      fe.Drop += OnDrop;
    } else {
      fe.PreviewMouseMove -= OnPreviewMouseMove;
      fe.Drop -= OnDrop;
      fe.AllowDrop = false;
    }
  }

  // 記住滑鼠按下位置，避免一點點移動就觸發 drag
  private static Point _start;
  private static bool _down;

  private static void OnPreviewMouseMove(object sender, MouseEventArgs e) {
    if (sender is not FrameworkElement fe) return;

    if (e.LeftButton == MouseButtonState.Pressed && !_down) {
      _down = true;
      _start = e.GetPosition(null);
      return;
    }

    if (e.LeftButton != MouseButtonState.Pressed) {
      _down = false;
      return;
    }

    var pos = e.GetPosition(null);
    if (Math.Abs(pos.X - _start.X) < SystemParameters.MinimumHorizontalDragDistance &&
        Math.Abs(pos.Y - _start.Y) < SystemParameters.MinimumVerticalDragDistance)
      return;

    // DataContext 必須是 BubbleViewModel
    if (fe.DataContext is not BubbleViewModel vm) return;

    _down = false;

    var data = new DataObject();
    data.SetData("OrbitBubble.BubbleId", vm.Id);

    DragDrop.DoDragDrop(fe, data, DragDropEffects.Move);
  }

  private static void OnDrop(object sender, DragEventArgs e) {
    if (sender is not FrameworkElement fe) return;
    if (fe.DataContext is not BubbleViewModel targetVm) return;

    if (!e.Data.GetDataPresent("OrbitBubble.BubbleId")) return;
    var sourceId = e.Data.GetData("OrbitBubble.BubbleId") as string;
    if (string.IsNullOrWhiteSpace(sourceId)) return;

    var targetId = targetVm.Id;
    if (sourceId == targetId) return;

    // 從 DI 取得 IBubbleService & MainViewModel 來操作
    // 你需要在 App 中把 ServiceProvider 暴露出來（見 B2）
    var sp = AppServices.Provider;
    var bubbleService = sp.GetRequiredService<IBubbleService>();
    var mainVm = sp.GetRequiredService<MainViewModel>();

    var r = bubbleService.MergeIntoCollection(sourceId, targetId);
    if (r.IsSuccess) {
      _ = mainVm.RefreshVisibleBubblesAsync();
    }
    // 失敗 log 交給 service 內部＋VM（需要的話你也可以在這邊加 logger）
  }
}
