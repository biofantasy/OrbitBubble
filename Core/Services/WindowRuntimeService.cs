using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace OrbitBubble.Core.Services;

public class WindowRuntimeService {
  public const int HotkeyMessage = 0x0312;
  private static readonly IntPtr HwndTopMost = new(-1);
  private const uint SwpNoSize = 0x0001;
  private const uint SwpNoMove = 0x0002;
  private const uint SwpNoActivate = 0x0010;
  private const uint SwpShowWindow = 0x0040;

  public bool IsHotkeyMessage(int msg, nint wParam, int hotkeyId) {
    return msg == HotkeyMessage && wParam.ToInt32() == hotkeyId;
  }

  public void EnsureTopMost(Window window) {
    var hwnd = new WindowInteropHelper(window).Handle;
    if (hwnd == IntPtr.Zero) return;

    SetWindowPos(hwnd, HwndTopMost, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate | SwpShowWindow);
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

  [DllImport("user32.dll", SetLastError = true)]
  private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
}
