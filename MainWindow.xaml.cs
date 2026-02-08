using Microsoft.Extensions.Logging;
using OrbitBubble.Core.ViewModels;
using OrbitBubble.Infrastructure.Input;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using OrbitBubble.Domain.Gestures;

namespace OrbitBubble;

public partial class MainWindow : Window {
  private const int WM_HOTKEY = 0x0312;
  private const int HOTKEY_ID = 9000; // 要跟 HotkeyManager 一致

  private readonly ILogger<MainWindow> _logger;
  private readonly MainViewModel _vm;
  private readonly IInputTriggerService _input;
  private readonly IGestureDetectionService _gesture;

  public MainWindow(
      ILogger<MainWindow> logger,
      MainViewModel vm,
      IInputTriggerService input,
      IGestureDetectionService gesture) {

    InitializeComponent();

    _logger = logger;
    _vm = vm;
    _input = input;
    _gesture = gesture;

    DataContext = _vm;

    Loaded += MainWindow_Loaded;
    Closed += MainWindow_Closed;
    SizeChanged += MainWindow_SizeChanged;
  }

  private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
    // ✅ 1) VM 初始化（Load bubbles → VisibleBubbles）
    _vm.Initialize();

    // ✅ 2) 設定 viewport，避免泡泡座標飛到負數看不到
    _vm.UpdateViewport(ActualWidth, ActualHeight);

    // ✅ 3) Hotkey message hook
    var hwnd = new WindowInteropHelper(this).Handle;
    var source = HwndSource.FromHwnd(hwnd);
    source.AddHook(WndProc);

    // ✅ 4) Input/Gesture 啟動
    _input.MenuRequested += Input_MenuRequested;
    _input.Start(hwnd);
    _gesture.Start();

    _logger.LogInformation("MainWindow loaded. hwnd={Hwnd}", hwnd);
  }

  private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) {
    // 視窗大小變了就重新佈局（不然會跑出畫面）
    _vm.UpdateViewport(ActualWidth, ActualHeight);
  }

  private void MainWindow_Closed(object? sender, EventArgs e) {
    try {
      _gesture.Stop();
      _input.Stop();
      _input.MenuRequested -= Input_MenuRequested;
    } catch (Exception ex) {
      _logger.LogError(ex, "Error while stopping services.");
    }
  }

  private void Input_MenuRequested(object? sender, MenuRequestedEventArgs e) {
    // 先全部都 toggle；你之後要 source 分流再加
    _vm.ToggleMenuCommand.Execute(null);
  }

  private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
    if (msg == WM_HOTKEY) {
      var id = wParam.ToInt32();
      if (id == HOTKEY_ID) {
        _input.RequestMenu(MenuRequestSource.Hotkey);
        handled = true;
      }
    }
    return IntPtr.Zero;
  }

  private void Bubble_Click(object sender, MouseButtonEventArgs e) {
    if (sender is FrameworkElement fe && fe.DataContext is BubbleViewModel bvm)
      _vm.OpenBubbleCommand.Execute(bvm.Id);
  }
}
