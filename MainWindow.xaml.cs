using OrbitBubble.Core.Managers;
using OrbitBubble.Core.Models;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OrbitBubble;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {

  private FrameworkElement? _draggedElement; // 記錄當前拖曳的物件
  private HotkeyManager _hotkeyManager = new();
  private bool _isDragging = false;
  private System.Windows.Point _clickPosition;

  public MainWindow() {

    InitializeComponent();

    // 監聽中心圓的右鍵
    CenterHub.MouseRightButtonUp += CenterHub_MouseRightButtonUp;
  }

  private void CenterHub_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
    var menu = new ContextMenu();

    var setHotkey = new MenuItem { Header = "設定熱鍵 (Alt+Space)" };
    var setGesture = new MenuItem { Header = "手勢靈敏度設定" };
    var exitApp = new MenuItem { Header = "結束程式" };
    exitApp.Click += (s, a) => Application.Current.Shutdown();

    menu.Items.Add(setHotkey);
    menu.Items.Add(setGesture);
    menu.Items.Add(new Separator());
    menu.Items.Add(exitApp);

    menu.IsOpen = true;
  }

  protected override void OnSourceInitialized(EventArgs e) {

    base.OnSourceInitialized(e);

    // 獲取視窗句柄 (HWND)
    var helper = new WindowInteropHelper(this);
    var hwnd = helper.Handle;

    // 註冊熱鍵
    _hotkeyManager.Register(hwnd);

    // 掛載訊息 Hook
    HwndSource source = HwndSource.FromHwnd(hwnd)!;
    source.AddHook(HwndHook);

    // 2. 放入拖曳事件初始化
    InitializeDragEvents();
  }

  private void InitializeDragEvents() {
    CenterHub.MouseLeftButtonDown += CenterHub_MouseLeftButtonDown;
    this.MouseMove += Window_MouseMove;
    this.MouseLeftButtonUp += Window_MouseLeftButtonUp;
  }

  private void CenterHub_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
    _isDragging = true;
    _clickPosition = e.GetPosition(this);
    CenterHub.CaptureMouse();

    // 1. 拖曳開始：隱藏泡泡 (可以使用淡出動畫，或直接隱藏)
    SetBubblesVisibility(Visibility.Hidden);
  }

  private void Window_MouseMove(object sender, MouseEventArgs e) {
    if (_isDragging) {
      System.Windows.Point currentPos = e.GetPosition(this);
      // 計算偏移量並移動視窗
      this.Left += currentPos.X - _clickPosition.X;
      this.Top += currentPos.Y - _clickPosition.Y;
    }
  }

  private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {

    if (_isDragging) {

      _isDragging = false;
      CenterHub.ReleaseMouseCapture();

      // --- 核心邏輯：偵測重疊 ---
      // 假設你正在拖曳的元件是 _draggedElement
      foreach (var other in MainCanvas.Children.OfType<Ellipse>()) {
        if (other == _draggedElement || other == CenterHub) continue;

        if (GetDistance(_draggedElement, other) < 40) // 距離小於 40 像素就合併
        {
          MergeBubbles(other, _draggedElement);
          break;
        }
      }
      // -----------------------

      SetBubblesOpacity(1);

      // 2. 拖曳結束：顯示泡泡並重新執行佈局動畫
      //SetBubblesVisibility(Visibility.Visible);
      // 如果想讓泡泡重新噴射出來，可以在這裡呼叫 LayoutBubbles
    }
  }

  private void SetBubblesOpacity(double targetOpacity) {
    foreach (var child in MainCanvas.Children) {
      if (child is Ellipse el && el != CenterHub) {
        DoubleAnimation da = new DoubleAnimation {
          To = targetOpacity,
          Duration = TimeSpan.FromSeconds(0.15)
        };
        el.BeginAnimation(OpacityProperty, da);
      }
    }
  }

  private void SetBubblesVisibility(Visibility visibility) {
    foreach (var child in MainCanvas.Children) {
      if (child is Ellipse el && el != CenterHub) {
        el.Visibility = visibility;
      }
    }
  }

  private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) {
    const int WM_HOTKEY = 0x0312;

    if (msg == WM_HOTKEY && wParam.ToInt32() == 9000) {
      OnHotkeyTriggered();
      handled = true;
    }
    return nint.Zero;
  }

  private void OnHotkeyTriggered() {

    // 如果視窗目前已經看得到，按第二次就隱藏
    if (this.Visibility == Visibility.Visible) {

      // 如果目前是開啟狀態，執行「縮小動畫」
      HideMenuWithAnimation();
    } else {

      // 1. 先定位到滑鼠位置
      UpdatePositionToMouse();

      // 2. 清理舊泡泡並重新生成數據
      ClearBubbles();
      LayoutBubbles(8);

      // 3. 執行「噴射彈出」動畫
      ShowMenuWithAnimation();

      this.Activate(); // 確保獲取焦點
    }
  }

  private void UpdatePositionToMouse() {

    System.Drawing.Point p;
    Windows.Win32.PInvoke.GetCursorPos(out p); // px

    var source = PresentationSource.FromVisual(this);
    if (source?.CompositionTarget != null) {
      var m = source.CompositionTarget.TransformFromDevice; // px -> DIP
      var dip = m.Transform(new System.Windows.Point(p.X, p.Y));

      this.Left = dip.X - (this.Width / 2);
      this.Top = dip.Y - (this.Height / 2);
    } else {
      // 視窗還沒建立視覺來源時的 fallback
      this.Left = p.X - (this.Width / 2);
      this.Top = p.Y - (this.Height / 2);
    }
  }

  private void HideMenuWithAnimation() {
    // 建立縮小動畫 (從 1.0 縮到 0.0)
    DoubleAnimation da = new DoubleAnimation {
      To = 0,
      Duration = TimeSpan.FromSeconds(0.2),
      EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
    };

    // 當動畫結束時，才真正隱藏視窗並清理泡泡
    da.Completed += (s, e) => {
      this.Visibility = Visibility.Collapsed;
      ClearBubbles(); // 移除動態加入的泡泡
    };

    MainScale.BeginAnimation(ScaleTransform.ScaleXProperty, da);
    MainScale.BeginAnimation(ScaleTransform.ScaleYProperty, da);
  }

  private void ClearBubbles() {
    // 為了不刪掉中心圓 (CenterHub)，我們只刪除 Ellipse 且不是 CenterHub 的物件
    var toRemove = MainCanvas.Children.OfType<Ellipse>()
                                     .Where(x => x != CenterHub)
                                     .ToList();
    foreach (var item in toRemove) {
      MainCanvas.Children.Remove(item);
    }
  }

  private void ShowMenuWithAnimation() {
    // 1. 先顯示視窗，但比例設為 0
    MainScale.ScaleX = 0;
    MainScale.ScaleY = 0;
    this.Visibility = Visibility.Visible;

    // 2. 放大動畫
    DoubleAnimation da = new DoubleAnimation {
      To = 1,
      Duration = TimeSpan.FromSeconds(0.3),
      EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 }
    };
    MainScale.BeginAnimation(ScaleTransform.ScaleXProperty, da);
    MainScale.BeginAnimation(ScaleTransform.ScaleYProperty, da);

    // 3. 生成泡泡
    LayoutBubbles(8);
  }

  private void LayoutBubbles(int count) {
    //double radius = 150; // 泡泡距離中心的距離
    //double centerX = this.Width / 2;
    //double centerY = this.Height / 2;

    //for (int i = 0; i < count; i++) {
    //  double angle = i * Math.PI * 2 / count; // 弧度計算
    //  double x = centerX + radius * Math.Cos(angle) - 25; // 25 為泡泡半徑偏移
    //  double y = centerY + radius * Math.Sin(angle) - 25;

    //  // 建立泡泡元件 (這裡暫時用圓圈代替)
    //  Ellipse bubble = new Ellipse {
    //    Width = 50, Height = 50,
    //    Fill = Brushes.DeepSkyBlue,
    //    Opacity = 0.8
    //  };

    //  Canvas.SetLeft(bubble, x);
    //  Canvas.SetTop(bubble, y);

    //  // 將泡泡加入你的 Canvas (假設名稱為 MainCanvas)
    //  MainCanvas.Children.Add(bubble);

    //  // TODO: 加入彈入動畫 (RenderTransform + DoubleAnimation)
    //}
  }

  private void CheckForMerger(UIElement draggedBubble) {
    foreach (var other in MainCanvas.Children.OfType<FrameworkElement>()) {
      if (other == draggedBubble || other == CenterHub) continue;

      // 簡單的圓心距離檢測
      if (GetDistance(draggedBubble, other) < 50) {
        // 執行合併邏輯：將 other 的路徑加入 draggedBubble 的 SubItems
        // 並從 Canvas 移除 other
        MergeBubbles(draggedBubble, other);
        break;
      }
    }
  }

  private double GetDistance(UIElement e1, UIElement e2) {
    // 取得第一個泡泡的中心點
    double x1 = Canvas.GetLeft(e1) + (e1 as FrameworkElement).ActualWidth / 2;
    double y1 = Canvas.GetTop(e1) + (e1 as FrameworkElement).ActualHeight / 2;

    // 取得第二個泡泡的中心點
    double x2 = Canvas.GetLeft(e2) + (e2 as FrameworkElement).ActualWidth / 2;
    double y2 = Canvas.GetTop(e2) + (e2 as FrameworkElement).ActualHeight / 2;

    // 計算歐幾里得距離
    return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
  }

  private void MergeBubbles(UIElement target, UIElement source) {
    // 假設我們在 Tag 屬性裡存了 BubbleItem 資料
    var targetData = target.GetValue(FrameworkElement.TagProperty) as BubbleItem;
    var sourceData = source.GetValue(FrameworkElement.TagProperty) as BubbleItem;

    if (targetData != null && sourceData != null) {
      // 1. 將來源資料加入目標的子清單
      targetData.SubItems.Add(sourceData);

      // 2. 更新視覺：讓目標泡泡看起來像個「集合」（例如加個外框或改變顏色）
      (target as Ellipse).StrokeThickness = 4;
      (target as Ellipse).Stroke = System.Windows.Media.Brushes.Gold;

      // 3. 從畫面上移除被合併的泡泡
      MainCanvas.Children.Remove(source);

      // 4. (進階) 播放一個小小的吸入動畫
      PlayMergeEffect(target);
    }
  }

  // 假設這是您動態生成泡泡時掛載的事件
  private void Bubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
    _draggedElement = sender as FrameworkElement; // 捕捉被點擊的泡泡
    _isDragging = true;
    _clickPosition = e.GetPosition(this);

    if (_draggedElement != null) {
      _draggedElement.CaptureMouse();
      // 提升 z-index 確保拖曳的泡泡在最上方
      Panel.SetZIndex(_draggedElement, 1000);
    }
  }

  private void PlayMergeEffect(UIElement target) {
    // 建立一個簡單的「震動」或「脈衝」效果，告知使用者合併成功
    DoubleAnimation pulse = new DoubleAnimation {
      To = 1.2, // 稍微放大
      Duration = TimeSpan.FromSeconds(0.1),
      AutoReverse = true, // 自動縮回原樣
      EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
    };

    // 取得目標泡泡的縮放轉換物件
    if (target.RenderTransform is ScaleTransform st) {
      st.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
      st.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }
  }

  // 在 MergeBubbles 內調用
  private void UpdateCollectionVisual(Ellipse target) {
    // 方案 A：加粗邊框並換色
    target.Stroke = System.Windows.Media.Brushes.Gold;
    target.StrokeThickness = 3;

    // 方案 B：使用虛線邊框模擬「容器感」
    target.StrokeDashArray = new DoubleCollection() { 2, 1 };

    // 播放提示動畫
    PlayMergeEffect(target);
  }
}