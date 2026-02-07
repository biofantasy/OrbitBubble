using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OrbitBubble.Core.Helpers;

public class GlobalMouseHook {

  private const int WH_MOUSE_LL = 14;
  private const int WM_MOUSEMOVE = 0x0200;

  [StructLayout(LayoutKind.Sequential)]
  public struct POINT { public int x; public int y; }

  [StructLayout(LayoutKind.Sequential)]
  public struct MSLLHOOKSTRUCT { public POINT pt; public uint mouseData, flags, time; public IntPtr dwExtraInfo; }

  public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
  private LowLevelMouseProc _proc;
  private IntPtr _hookId = IntPtr.Zero;

  public event Action<int, int> MouseMoved;

  public void Install() {
    _proc = HookCallback;
    // 使用 IntPtr.Zero 通常在這種情況下更穩定
    _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, IntPtr.Zero, 0);

    if (_hookId == IntPtr.Zero) {
      int errorCode = Marshal.GetLastWin32Error();
      // 如果失敗，可以在這裡記錄錯誤碼碼
    }
  }

  public void Uninstall() {
    UnhookWindowsHookEx(_hookId);
  }

  // 這個方法放在類別內
  private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
    if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEMOVE) {
      MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

      // 非同步發出事件，不阻塞滑鼠移動
      int x = hookStruct.pt.x;
      int y = hookStruct.pt.y;
      Task.Run(() => MouseMoved?.Invoke(x, y));
    }
    return CallNextHookEx(_hookId, nCode, wParam, lParam);
  }

  [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
  [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
  [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
  [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string lpModuleName);
}
