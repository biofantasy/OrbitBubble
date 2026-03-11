using System.Windows;
using System.Windows.Media;

namespace OrbitBubble.Core.Helpers;

public static class UiElementExtensions {
  public static Task NextFrame(this UIElement element) {
    var tcs = new TaskCompletionSource<object?>();
    EventHandler handler = null!;
    handler = (_, _) => {
      CompositionTarget.Rendering -= handler;
      tcs.TrySetResult(null);
    };
    CompositionTarget.Rendering += handler;
    return tcs.Task;
  }
}
