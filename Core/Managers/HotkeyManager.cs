using Windows.Win32; // 基礎定義
using Windows.Win32.UI.Input.KeyboardAndMouse; // 包含 HOTKEY_MODIFIERS

namespace OrbitBubble.Core.Managers;

public class HotkeyManager {

  private const int HOTKEY_ID = 9000; // 唯一的熱鍵 ID
  private nint _windowHandle;

  // 註冊熱鍵：預設為 Alt + Space
  public void Register(nint handle) {

    _windowHandle = handle;
    
    // MOD_ALT | MOD_NOREPEAT (避免按住不放重複觸發)
    var modifiers = HOT_KEY_MODIFIERS.MOD_ALT | HOT_KEY_MODIFIERS.MOD_NOREPEAT;

    // 0x20 是 Space 的虛擬鍵碼 (Virtual Key Code)
    // PInvoke => CsWin32 DllImport
    bool success = PInvoke.RegisterHotKey((Windows.Win32.Foundation.HWND)handle, HOTKEY_ID, modifiers, 0x20);

    if (!success) {
      // 這裡可以加入 log 或提示熱鍵被佔用
      System.Diagnostics.Debug.WriteLine("熱鍵註冊失敗！");
    }
  }

  public void Unregister() {
    PInvoke.UnregisterHotKey((Windows.Win32.Foundation.HWND)_windowHandle, HOTKEY_ID);
  }
}

