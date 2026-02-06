using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OrbitBubble.Core.Helpers;

public static class IconHelper {
  // 調用 Win32 API 獲取檔案資訊
  [DllImport("shell32.dll", CharSet = CharSet.Auto)]
  private static extern nint SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

  [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
  private struct SHFILEINFO {
    public nint hIcon;
    public int iIcon;
    public uint dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
  }

  private const uint SHGFI_ICON = 0x100;
  private const uint SHGFI_LARGEICON = 0x0; // 32x32
  private const uint SHGFI_USEFILEATTRIBUTES = 0x10;

  public static ImageSource GetIcon(string path) {
    SHFILEINFO shfi = new SHFILEINFO();
    // 獲取大圖示
    SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), SHGFI_ICON | SHGFI_LARGEICON);

    if (shfi.hIcon == nint.Zero) return null;

    // 將 hIcon 轉換為 WPF 的 ImageSource
    ImageSource icon = Imaging.CreateBitmapSourceFromHIcon(
        shfi.hIcon,
        Int32Rect.Empty,
        BitmapSizeOptions.FromEmptyOptions());

    // 記得釋放 Win32 資源，避免記憶體洩漏
    DestroyIcon(shfi.hIcon);

    return icon;
  }

  [DllImport("user32.dll", SetLastError = true)]
  private static extern bool DestroyIcon(nint hIcon);
}
