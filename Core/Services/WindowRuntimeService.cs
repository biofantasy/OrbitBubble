using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace OrbitBubble.Core.Services;

public class WindowRuntimeService {
  public const int HotkeyMessage = 0x0312;

  public bool IsHotkeyMessage(int msg, nint wParam, int hotkeyId) {
    return msg == HotkeyMessage && wParam.ToInt32() == hotkeyId;
  }

  public bool TryUpdateWrapperToMouse(Window window, Canvas animationWrapper, double stageCenterX, double stageCenterY) {
    if (!Windows.Win32.PInvoke.GetCursorPos(out var p)) {
      return false;
    }

    var source = PresentationSource.FromVisual(window);
    if (source?.CompositionTarget == null) {
      return false;
    }

    var m = source.CompositionTarget.TransformFromDevice;
    var dipMousePos = m.Transform(new Point(p.X, p.Y));
    var wrapperPos = CalculateWrapperPosition(
      dipMousePos,
      new Point(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop),
      new Point(stageCenterX, stageCenterY));

    Canvas.SetLeft(animationWrapper, wrapperPos.X);
    Canvas.SetTop(animationWrapper, wrapperPos.Y);
    return true;
  }

  /// <summary>
  /// 計算 AnimationWrapper 左上角位置，讓指定軌道中心對齊滑鼠位置。
  /// </summary>
  public Point CalculateWrapperPosition(Point dipMousePos, Point virtualScreenOrigin, Point orbitCenter) {
    double mouseOnCanvasX = dipMousePos.X - virtualScreenOrigin.X;
    double mouseOnCanvasY = dipMousePos.Y - virtualScreenOrigin.Y;
    return new Point(mouseOnCanvasX - orbitCenter.X, mouseOnCanvasY - orbitCenter.Y);
  }
}
