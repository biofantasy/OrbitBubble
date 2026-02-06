using OrbitBubble.Core.Helpers;
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

  // 記錄導航路徑，如果是空的代表在「根目錄」
  private Stack<List<BubbleItem>> _navHistory = new();
  private List<BubbleItem> _currentViewBubbles = new(); // 當前畫面上顯示的資料
  private List<BubbleItem> _allBubbles = new(); // 儲存所有加入的泡泡
  private FrameworkElement? _draggedElement; // 記錄當前拖曳的物件
  private HotkeyManager _hotkeyManager = new();
  private bool _isDragging = false;
  private System.Windows.Point _clickPosition;

  public MainWindow() {

    InitializeComponent();

    // 1. 設定視窗覆蓋所有螢幕
    this.Left = SystemParameters.VirtualScreenLeft;
    this.Top = SystemParameters.VirtualScreenTop;
    this.Width = SystemParameters.VirtualScreenWidth;
    this.Height = SystemParameters.VirtualScreenHeight;

    // 2. 關鍵：確保畫布也一樣大，否則 AddBubble 計算會出錯
    MainCanvas.Width = this.Width;
    MainCanvas.Height = this.Height;

    (_allBubbles, _currentViewBubbles) = BubbleDataManager.LoadData(); // 程式啟動先載入
    // 監聽中心圓的右鍵
    CenterHub.MouseRightButtonUp += CenterHub_MouseRightButtonUp;

    // 初始化時先給 CenterHub 一個畫面中間的位置，避免一開始是 NaN
    Canvas.SetLeft(CenterHub, this.Width / 2 - 50);
    Canvas.SetTop(CenterHub, this.Height / 2 - 50);

    RefreshLayout();
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

  /// <summary>
  /// 拖曳事件初始化
  /// </summary>
  private void InitializeDragEvents() {

    CenterHub.MouseLeftButtonDown += CenterHub_MouseLeftButtonDown;

    this.MouseMove += Window_MouseMove;

    this.MouseLeftButtonUp += Window_MouseLeftButtonUp;
  }

  private void CenterHub_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

    // 如果目前在子層級，且只是輕點一下 (ClickCount == 1)
    if (_navHistory.Count > 0 && e.ClickCount == 1) {
      BackToParent();
      e.Handled = true;
      return;
    }

    _isDragging = true;
    _draggedElement = CenterHub;
    _clickPosition = e.GetPosition(this);

    // 關鍵：Capture 必須針對被點擊的物件
    CenterHub.CaptureMouse();

    // 拖動時可以隱藏泡泡，或者降低透明度
    SetBubblesOpacity(0.3);
  }

  private void Window_MouseMove(object sender, MouseEventArgs e) {
    if (_isDragging && _draggedElement != null) {
      System.Windows.Point currentPos = e.GetPosition(this);
      double deltaX = currentPos.X - _clickPosition.X;
      double deltaY = currentPos.Y - _clickPosition.Y;

      // 無論是中心圓還是泡泡，統一移動 Canvas 座標
      double left = Canvas.GetLeft(_draggedElement);
      double top = Canvas.GetTop(_draggedElement);

      // 防呆：如果是 NaN 則初始化位置
      if (double.IsNaN(left)) left = 0;
      if (double.IsNaN(top)) top = 0;

      Canvas.SetLeft(_draggedElement, left + deltaX);
      Canvas.SetTop(_draggedElement, top + deltaY);

      // 重要：每次移動後更新基準點，否則會噴射
      _clickPosition = currentPos;
    }
  }

  // 2. 放開時：嚴格判定位移
  private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {

    if (_isDragging && _draggedElement != null) {

      _isDragging = false;
      _draggedElement.ReleaseMouseCapture();

      if (_draggedElement == CenterHub) {
        // 中心圓拖完，立即重新計算所有泡泡的位置
        RefreshLayout();
      } else {
        // 泡泡拖完，檢查合併或回彈
        bool isMerged = CheckForMerger(_draggedElement);
        if (!isMerged) {
          ReturnBubbleToOrbit(_draggedElement);
          //RefreshLayout();
        }
      }

      SetBubblesOpacity(1);
      _draggedElement = null;
    }
  }

  // 新增回彈動畫方法
  private void ReturnBubbleToOrbit(FrameworkElement element) {
    var data = element.Tag as BubbleItem;
    if (data == null) return;

    int index = _currentViewBubbles.IndexOf(data);
    int total = _currentViewBubbles.Count;

    double hubX = Canvas.GetLeft(CenterHub);
    double hubY = Canvas.GetTop(CenterHub);
    double centerX = hubX + (CenterHub.ActualWidth / 2);
    double centerY = hubY + (CenterHub.ActualHeight / 2);

    double radius = 180;
    double angle = index * Math.PI * 2 / total;
    double targetX = centerX + radius * Math.Cos(angle) - (element.ActualWidth / 2);
    double targetY = centerY + radius * Math.Sin(angle) - (element.ActualHeight / 2);

    double currentX = Canvas.GetLeft(element);
    double currentY = Canvas.GetTop(element);

    // 建立動畫
    var animX = new DoubleAnimation(currentX, targetX, TimeSpan.FromSeconds(0.4)) {
      EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
    };
    var animY = new DoubleAnimation(currentY, targetY, TimeSpan.FromSeconds(0.4)) {
      EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
    };

    // 關鍵：當動畫結束時，將座標「固化」
    animX.Completed += (s, e) => {
      element.BeginAnimation(Canvas.LeftProperty, null); // 移除動畫鎖
      Canvas.SetLeft(element, targetX); // 正式寫入本地值
    };
    animY.Completed += (s, e) => {
      element.BeginAnimation(Canvas.TopProperty, null); // 移除動畫鎖
      Canvas.SetTop(element, targetY); // 正式寫入本地值
    };

    element.BeginAnimation(Canvas.LeftProperty, animX);
    element.BeginAnimation(Canvas.TopProperty, animY);
  }

  private void SetBubblesOpacity(double opacity) {
    // 使用 ToList() 建立快照，避免遍歷時因集合變動而崩潰
    var bubbles = MainCanvas.Children.OfType<UIElement>()
                                    .Where(x => x != CenterHub)
                                    .ToList();

    foreach (var el in bubbles) {
      DoubleAnimation da = new DoubleAnimation {
        To = opacity,
        Duration = TimeSpan.FromSeconds(0.2),
        EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
      };
      el.BeginAnimation(UIElement.OpacityProperty, da);
    }
  }

  private void SetBubblesVisibility(Visibility visibility) {
    foreach (var child in MainCanvas.Children) {
      if (child is FrameworkElement el && el != CenterHub) {
        el.Visibility = visibility;
      }
    }
  }

  private void ExecuteBubbleAction(FrameworkElement element) {
    var data = element.Tag as BubbleItem;
    if (data == null || string.IsNullOrEmpty(data.Path)) return;

    try {
      // 如果是集合，則執行展開邏輯（我們之前討論過的切換層級）
      // 關鍵：如果 SubItems 有東西，或者是我們定義的 "Collection" 路徑
      if (data.SubItems.Count > 0 || data.Path == "Collection") {
        ExpandCollection(data); // 進入子層級
      } else {
        // 如果是單一檔案或資料夾，直接開啟
        var psi = new System.Diagnostics.ProcessStartInfo {
          FileName = data.Path,
          UseShellExecute = true // 使用作業系統外殼執行（重要！）
        };
        System.Diagnostics.Process.Start(psi);

        // 開啟後可以選擇自動隱藏選單
        HideMenuWithAnimation();
      }
    } catch (Exception ex) {
      System.Windows.MessageBox.Show($"無法開啟檔案: {ex.Message}");
    }
  }

  private void ExpandCollection(BubbleItem collection) {

    // 1. 儲存目前內容到歷史，以便回退
    _navHistory.Push(new List<BubbleItem>(_currentViewBubbles));

    // 將當前資料源替換為集合內的子項目
    _currentViewBubbles = collection.SubItems;

    // 4. 更新中間 Menu 顯示名稱
    UpdateCenterHubText(collection.Name);

    // 3. 更新 UI
    RefreshLayout();
  }

  private void UpdateCenterHubText(string text) {

    HubCircle.Stroke = System.Windows.Media.Brushes.Gold; // 進入集合變金色
    // 假設您的 CenterHub 裡面有一個 TextBlock 叫 HubText
    // 如果沒有，您可以在 XAML 的 CenterHub (Grid) 裡加一個
    HubText.Text = text;
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

      // 每次開啟都重置導航到最頂層
      _navHistory.Clear();
      // 確保目前視角等於所有泡泡
      _currentViewBubbles = new List<BubbleItem>(_allBubbles);

      // 修正這裡：控制內部的 Ellipse 物件
      HubCircle.Stroke = System.Windows.Media.Brushes.Cyan;
      HubText.Text = "Root";

      RefreshLayout();

      // 3. 執行「噴射彈出」動畫
      ShowMenuWithAnimation();

      this.Activate(); // 確保獲取焦點
    }
  }

  private void UpdatePositionToMouse() {
    System.Drawing.Point p;
    Windows.Win32.PInvoke.GetCursorPos(out p);

    var source = PresentationSource.FromVisual(this);
    if (source?.CompositionTarget != null) {
      var m = source.CompositionTarget.TransformFromDevice;
      var dip = m.Transform(new System.Windows.Point(p.X, p.Y));

      // 核心修正：計算相對於整個虛擬畫布的座標
      // 因為 this.Left 可能是負值（副螢幕在左邊），dip.X 也是絕對座標
      // 我們直接求出兩者的差值，這就是滑鼠在畫布上的正確位置
      double mouseOnCanvasX = dip.X - this.Left;
      double mouseOnCanvasY = dip.Y - this.Top;

      // 1. 強制重置 CenterHub 的位置，不繼承上次的任何狀態
      Canvas.SetLeft(CenterHub, mouseOnCanvasX - (CenterHub.ActualWidth / 2));
      Canvas.SetTop(CenterHub, mouseOnCanvasY - (CenterHub.ActualHeight / 2));

      // 2. 讓動畫從滑鼠點「噴發」出來
      MainScale.CenterX = mouseOnCanvasX;
      MainScale.CenterY = mouseOnCanvasY;
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
    // 修正：現在要找的是 Grid (CreateBubbleVisual 回傳的容器)
    var toRemove = MainCanvas.Children.OfType<FrameworkElement>()
                                     .Where(x => x != CenterHub)
                                     .ToList();
    foreach (var item in toRemove) {
      MainCanvas.Children.Remove(item);
    }
  }

  private void ShowMenuWithAnimation() {

    this.Visibility = Visibility.Visible;
    this.Opacity = 1;

    // 1. 強制重置 Scale 數值，防止被上次的隱藏動畫鎖死
    MainScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    MainScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

    DoubleAnimation da = new DoubleAnimation {
      From = 0, // 強制從 0 開始
      To = 1,
      Duration = TimeSpan.FromSeconds(0.3),
      EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
    };

    MainScale.BeginAnimation(ScaleTransform.ScaleXProperty, da);
    MainScale.BeginAnimation(ScaleTransform.ScaleYProperty, da);
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

  private bool CheckForMerger(UIElement draggedBubble) {
    var allItems = MainCanvas.Children.OfType<FrameworkElement>().ToList();

    foreach (var other in allItems) {
      if (other == draggedBubble || other == CenterHub) continue;

      // 檢查距離是否小於 50 像素
      if (other.Tag is BubbleItem && GetDistance(draggedBubble, other) < 50) {
        MergeBubbles(other, draggedBubble);
        BubbleDataManager.SaveData(_allBubbles); // 合併成功即存檔
        return true; // 告知合併成功
      }
    }
    return false; // 沒有發生合併
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
    var targetFE = target as FrameworkElement;
    var sourceFE = source as FrameworkElement;
    if (targetFE == null || sourceFE == null) return;

    var targetData = targetFE.Tag as BubbleItem;
    var sourceData = sourceFE.Tag as BubbleItem;
    if (targetData == null || sourceData == null) return;

    // 1. 建立新的集合物件
    var collectionData = new BubbleItem {
      Name = "Merge",
      Path = "Collection",
      SubItems = new List<BubbleItem>()
    };

    // 2. 處理子項目的轉移 (處理原本就是集合的情況)
    if (targetData.SubItems != null && targetData.SubItems.Count > 0)
      collectionData.SubItems.AddRange(targetData.SubItems);
    else
      collectionData.SubItems.Add(targetData);

    if (sourceData.SubItems != null && sourceData.SubItems.Count > 0)
      collectionData.SubItems.AddRange(sourceData.SubItems);
    else
      collectionData.SubItems.Add(sourceData);

    // 3. 重要：從「當前顯示清單」中移除這兩個，並換成新的集合
    // 這樣其他的泡泡就會被保留在 _currentViewBubbles 中
    _currentViewBubbles.Remove(targetData);
    _currentViewBubbles.Remove(sourceData);
    _currentViewBubbles.Add(collectionData);

    // 4. 同步更新全域資料庫 (如果你在根目錄合併)
    if (_navHistory.Count == 0) {
      _allBubbles.Remove(targetData);
      _allBubbles.Remove(sourceData);
      _allBubbles.Add(collectionData);
    }

    // 5. 重新佈局 (這會根據更新後的 _currentViewBubbles 畫出所有剩下的泡泡)
    RefreshLayout();

    PlayMergeEffect(CenterHub);

    BubbleDataManager.SaveData(_allBubbles);
  }

  private void RefreshLayout() {

    ClearBubbles(); // 清空畫面

    for (int i = 0; i < _currentViewBubbles.Count; i++) {
      AddBubble(_currentViewBubbles[i], i, _currentViewBubbles.Count);
    }
  }

  // 假設這是您動態生成泡泡時掛載的事件
  // 1. 按下時：鎖定初始點
  private void Bubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

    _draggedElement = sender as FrameworkElement; // 捕捉被點擊的泡泡
    if (_draggedElement == null) return;

    // 關鍵：停止 Canvas.Left/Top 上的動畫，讓手動拖曳可以介入
    _draggedElement.BeginAnimation(Canvas.LeftProperty, null);
    _draggedElement.BeginAnimation(Canvas.TopProperty, null);

    _clickPosition = e.GetPosition(this);

    if (e.ClickCount == 2) { // 雙擊偵測
      _isDragging = false;
      _draggedElement.ReleaseMouseCapture();
      ExecuteBubbleAction(_draggedElement); // 執行進入或打開
      e.Handled = true;
      return;
    }

    // 單擊則進入拖曳準備
    _isDragging = true;
    _draggedElement.CaptureMouse();
    Panel.SetZIndex(_draggedElement, 1000);
    e.Handled = true;
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

  private void AddBubble(BubbleItem data, int index, int totalCount) {
    var bubble = CreateBubbleVisual(data);

    // 必須即時獲取 CenterHub 的 Canvas 座標
    double hubX = Canvas.GetLeft(CenterHub);
    double hubY = Canvas.GetTop(CenterHub);

    double centerX = hubX + 50;
    double centerY = hubY + 50;

    double radius = 180;
    double angle = index * Math.PI * 2 / totalCount;

    // 計算泡泡位置
    double x = centerX + radius * Math.Cos(angle) - 37.5;
    double y = centerY + radius * Math.Sin(angle) - 37.5;

    Canvas.SetLeft(bubble, x);
    Canvas.SetTop(bubble, y);
    MainCanvas.Children.Add(bubble);

    // 噴射動畫
    DoubleAnimation expand = new DoubleAnimation {
      To = 1.0,
      Duration = TimeSpan.FromSeconds(0.3),
      BeginTime = TimeSpan.FromSeconds(index * 0.05),
      EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
    };
    bubble.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, expand);
    bubble.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, expand);
  }

  // 建立一個方法來產生泡泡 UI
  private FrameworkElement CreateBubbleVisual(BubbleItem data) {
    // 建立容器
    Grid container = new Grid {
      Width = 75, Height = 75,
      Background = System.Windows.Media.Brushes.Transparent, // <--- 必須加這個，拖曳才靈敏
      RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
      RenderTransform = new ScaleTransform(0, 0),
      Tag = data
    };

    // 1. 底色圓圈 (稍微加點發光感)
    Ellipse circle = new Ellipse {
      Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 20, 20, 20)),
      Stroke = System.Windows.Media.Brushes.Cyan,
      StrokeThickness = 1.5,
      Effect = new System.Windows.Media.Effects.DropShadowEffect {
        Color = Colors.Cyan, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.5
      }
    };

    // 2. 檔案圖示
    System.Windows.Controls.Image img = new System.Windows.Controls.Image {
      Source = IconHelper.GetIcon(data.Path), // 核心：提取圖示
      Width = 32, Height = 32,
      VerticalAlignment = VerticalAlignment.Center,
      Margin = new Thickness(0, 0, 0, 15) // 往上移一點，留空間給文字
    };

    // 判斷是否為集合
    if (data.SubItems.Count > 0 || data.Path == "Collection") {
      // 顯示 Windows 預設資料夾圖示 (可以使用之前寫的 IconHelper 抓一個空資料夾的路徑)
      string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
      img.Source = IconHelper.GetIcon(folderPath);
      data.Name = "Merge"; // 強制顯示 Merge 字樣
      circle.Stroke = System.Windows.Media.Brushes.Gold;
    } else {
      img.Source = IconHelper.GetIcon(data.Path);
    }

    // 3. 檔名文字 (縮小並放在底部)
    TextBlock txt = new TextBlock {
      Text = data.Name,
      Foreground = System.Windows.Media.Brushes.White,
      FontSize = 9,
      VerticalAlignment = VerticalAlignment.Bottom,
      HorizontalAlignment = HorizontalAlignment.Center,
      Margin = new Thickness(5, 0, 5, 8),
      TextTrimming = TextTrimming.CharacterEllipsis,
      MaxWidth = 60
    };

    // 確保內部的元件都不會攔截滑鼠事件，讓事件統一由 container (Grid) 處理
    circle.IsHitTestVisible = false;
    img.IsHitTestVisible = false;
    txt.IsHitTestVisible = false;

    container.Children.Add(circle);
    container.Children.Add(img);
    container.Children.Add(txt);

    // 在 container.MouseLeftButtonDown 附近加入
    container.MouseRightButtonUp += (s, e) => {
      var menu = new ContextMenu();
      var deleteItem = new MenuItem { Header = "刪除此泡泡", Foreground = System.Windows.Media.Brushes.Red };
      deleteItem.Click += (ms, ma) => {
        _allBubbles.Remove(data);
        _currentViewBubbles.Remove(data);
        RefreshLayout();
        BubbleDataManager.SaveData(_allBubbles);
      };
      menu.Items.Add(deleteItem);
      menu.IsOpen = true;
      e.Handled = true; // 防止觸發背景事件
    };

    // 掛載事件
    container.MouseLeftButtonDown += Bubble_MouseLeftButtonDown;

    return container;
  }

  private void CenterHub_DragOver(object sender, DragEventArgs e) {
    // 檢查拉進來的是不是檔案
    if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
      e.Effects = DragDropEffects.Copy; // 游標會變成「+」
      e.Handled = true; // 告知系統此事件已處理
    }
  }

  private void CenterHub_Drop(object sender, DragEventArgs e) {
    if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
      string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

      // 篩選掉已經存在的路徑
      var newFiles = files.Where(f => !_allBubbles.Any(b => b.Path == f)).ToList();

      if (newFiles.Count == 0) return; // 如果全部都重複，就不執行

      // 1. 先獲取目前的總數基數
      int baseCount = _allBubbles.Count;
      int newTotal = baseCount + files.Length;

      for (int i = 0; i < files.Length; i++) {
        var newItem = new BubbleItem {
          Name = System.IO.Path.GetFileName(files[i]),
          Path = files[i]
        };

        _allBubbles.Add(newItem);

        // 2. 這裡的 index 必須是 (基礎數量 + 當前循環次數)
        // 3. 這裡的 totalCount 必須是 (原本的 + 這次所有要加的)
        AddBubble(newItem, baseCount + i, newTotal);
      }

      BubbleDataManager.SaveData(_allBubbles);
    }
  }

  private void BackToParent() {
    if (_navHistory.Count > 0) {
      // 1. 回復上一層數據
      _currentViewBubbles = _navHistory.Pop();

      // 2. 恢復 CenterHub 視覺：變回青藍色
      HubCircle.Stroke = System.Windows.Media.Brushes.Cyan;
      HubText.Text = "Root"; // 回到主層級清空文字

      // 3. 刷新畫面
      RefreshLayout();
    }
  }

}