using Microsoft.Extensions.Logging;
using OrbitBubble.ViewModels;
using OrbitBubble.Domain.Gestures;
using OrbitBubble.Infrastructure.Input;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OrbitBubble;

public partial class MainWindow : Window {
  private readonly ILogger<MainWindow> _logger;
  private readonly MainViewModel _vm;
  private readonly IInputTriggerService _input;
  private readonly IGestureDetectionService _gesture;

  private bool _isOpen;

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
  }

  private void MainWindow_Loaded(object sender, RoutedEventArgs e) {

    _vm.Initialize();

    var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
    _input.Start(hwnd);                     // ✅ 必加：註冊 hotkey & hook

    _input.MenuRequested += Input_MenuRequested;
    _gesture.Start();

    Hide();
    _isOpen = false;

    CompositionTarget.Rendering += OnRendering;
  }

  private void MainWindow_Closed(object? sender, EventArgs e) {
    CompositionTarget.Rendering -= OnRendering;

    _gesture.Stop();
    _input.MenuRequested -= Input_MenuRequested;
  }

  private void Input_MenuRequested(object? sender, MenuRequestedEventArgs e) {
    // 觸發即以滑鼠為中心 toggle
    ShowAtMouseAndToggle();
  }

  private void ShowAtMouseAndToggle() {

    var p = GetMousePositionInScreenDip(this);

    if (!_isOpen) {
      // 1) 把 Hub 放在 overlay 正中心（VM 內部是 overlay local）
      _vm.SetCenter(_vm.OverlaySize / 2, _vm.OverlaySize / 2);

      // 2) 視窗定位：讓 overlay 的中心對準滑鼠
      Left = p.X - _vm.OverlaySize / 2;
      Top = p.Y - _vm.OverlaySize / 2;

      Show();
      Activate();
      _isOpen = true;

      _vm.IsMenuOpen = true;
      _ = _vm.RefreshVisibleBubblesAsync();
    } else {
      Hide();
      _isOpen = false;

      _vm.IsMenuOpen = false;
    }
  }

  private void OnRendering(object? sender, EventArgs e) {
    if (!_isOpen) return;

    // 每幀推進角度（你可改成依時間差）
    _vm.AngleOffset += _vm.AngularSpeed;
    _vm.UpdateBubblePositions();
  }

  private static Point GetMousePositionInScreenDip(Window anyWindow) {
    Windows.Win32.PInvoke.GetCursorPos(out var pt); // pt.X / pt.Y 是 device pixels
    var p = new Point(pt.X, pt.Y);

    var source = PresentationSource.FromVisual(anyWindow);
    if (source?.CompositionTarget != null)
      return source.CompositionTarget.TransformFromDevice.Transform(p);

    return p;
  }

  // ====== Drag & Drop: Explorer -> 加入 bubble ======
  private void Root_DragOver(object sender, DragEventArgs e) {
    if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
      e.Effects = DragDropEffects.Copy;
      e.Handled = true;
      return;
    }

    e.Effects = DragDropEffects.None;
    e.Handled = true;
  }

  private void Root_Drop(object sender, DragEventArgs e) {
    if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

    var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop);
    if (paths == null || paths.Length == 0) return;

    int added = 0;
    foreach (var path in paths) {
      if (System.IO.Directory.Exists(path)) {
        if (_vm.AddFolder(path)) added++;
      } else if (System.IO.File.Exists(path)) {
        if (_vm.AddFile(path)) added++;
      }
    }

    if (added > 0)
      _ = _vm.RefreshVisibleBubblesAsync();
  }

  private void Bubble_Click(object sender, MouseButtonEventArgs e) {
    if (sender is FrameworkElement fe && fe.DataContext is BubbleViewModel bvm)
      _vm.OpenBubbleCommand.Execute(bvm.Id);
  }

  private void Bubble_DoubleClick(object sender, MouseButtonEventArgs e) {
    if (sender is FrameworkElement fe && fe.DataContext is BubbleViewModel bvm)
      _vm.OpenBubbleCommand.Execute(bvm.Id);
  }

  private void Hub_LeftClick(object sender, MouseButtonEventArgs e) {
    if (_vm.Back())
      _ = _vm.RefreshVisibleBubblesAsync();
  }

  private void Hub_RightClick(object sender, MouseButtonEventArgs e) {
    // 你規格：中心圈右鍵開設定（先留 stub）
    _logger.LogInformation("Hub right click -> open settings (todo).");
  }

  private Point _dragStart;
  private bool _dragPrimed;

  private void Bubble_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
    _dragStart = e.GetPosition(this);
    _dragPrimed = true;
  }

  private void Bubble_PreviewMouseMove(object sender, MouseEventArgs e) {
    if (!_dragPrimed) return;
    if (e.LeftButton != MouseButtonState.Pressed) { _dragPrimed = false; return; }

    var pos = e.GetPosition(this);
    if (Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
        Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
      return;

    _dragPrimed = false;

    if (sender is FrameworkElement fe && fe.DataContext is BubbleViewModel bvm) {
      var data = new DataObject();
      data.SetData("OrbitBubble.BubbleId", bvm.Id);
      DragDrop.DoDragDrop(fe, data, DragDropEffects.Move);
    }
  }

  private void Bubble_Drop(object sender, DragEventArgs e) {
    // 外部拖進來（Explorer）不要走 merge，交給 Root_Drop
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
      return;

    if (!e.Data.GetDataPresent("OrbitBubble.BubbleId"))
      return;

    var sourceId = e.Data.GetData("OrbitBubble.BubbleId") as string;
    if (string.IsNullOrWhiteSpace(sourceId))
      return;

    if (sender is not FrameworkElement fe || fe.DataContext is not BubbleViewModel targetVm)
      return;

    if (sourceId == targetVm.Id)
      return;

    var r = _vm.Merge(sourceId, targetVm.Id);
    if (r)
      _ = _vm.RefreshVisibleBubblesAsync();
  }

  private void Bubble_DragOver(object sender, DragEventArgs e) {
    // 1) Explorer 外部拖檔/資料夾：不要在 bubble 上處理（交給 Root_Drop）
    if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
      e.Effects = DragDropEffects.None;
      e.Handled = true;
      return;
    }

    // 2) 內部拖曳（bubble -> bubble）合併
    if (e.Data.GetDataPresent("OrbitBubble.BubbleId")) {
      e.Effects = DragDropEffects.Move;
      e.Handled = true;
      return;
    }

    e.Effects = DragDropEffects.None;
    e.Handled = true;
  }

}
