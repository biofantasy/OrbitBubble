using System;
using System.Collections.Generic;
using System.Text;

namespace OrbitBubble.Core.Helpers;

public static class RenderHelper {
  public static System.Threading.Tasks.Task NextFrameAsync() {
    var tcs = new System.Threading.Tasks.TaskCompletionSource<object?>();
    EventHandler handler = null!;
    handler = (s, e) => {
      System.Windows.Media.CompositionTarget.Rendering -= handler; // 觸發一次後立即解綁
      tcs.TrySetResult(null);
    };
    System.Windows.Media.CompositionTarget.Rendering += handler;
    return tcs.Task;
  }
}

