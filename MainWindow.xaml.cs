using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Managers;
using OrbitBubble.Core.Models;
using OrbitBubble.Core.Repositories;
using OrbitBubble.Core.Services;
using OrbitBubble.Controls;
using OrbitBubble.Views;
using Microsoft.VisualBasic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OrbitBubble;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {

  private FrameworkElement? _draggedElement; // 記錄當前拖曳的物件
  private readonly IHotkeyManager _hotkeyManager;
  private readonly IBubbleRepository _bubbleRepository;
  private readonly GestureService _gestureService;
  private readonly BubbleViewFactory _bubbleViewFactory;
  private readonly BubbleLayoutService _bubbleLayoutService;
  private readonly BubbleInteractionService _bubbleInteractionService;
  private readonly BubbleValidationService _bubbleValidationService;
  private readonly BubbleStateService _bubbleStateService;
  private readonly MenuAnimationService _menuAnimationService;
  private readonly MenuFactory _menuFactory;
  private readonly WindowRuntimeService _windowRuntimeService;
  private bool _gestureEnabled = true;
  private bool _isDragging = false;
  private System.Windows.Point _clickPosition;
  private readonly IGlobalMouseHook _globalHook;
  private UiQualityMode _qualityMode = UiQualityMode.Balanced;
  private readonly object _mouseMoveSync = new();
  private System.Windows.Point _queuedMousePoint;
  private bool _hasQueuedMousePoint;
  private bool _mouseMoveDispatchScheduled;

  public MainWindow()
    : this(CreateDefaultDependencies()) {
  }

  private static MainWindowDependencies CreateDefaultDependencies() {
    var iconCache = new IconCacheService();
    var menuFactory = new MenuFactory();
    var bubbleValidationService = new BubbleValidationService();
    return new MainWindowDependencies(
      new HotkeyManager(),
      new BubbleRepository(),
      new GestureService(),
      new BubbleViewFactory(iconCache, menuFactory, bubbleValidationService),
      new BubbleLayoutService(),
      new BubbleInteractionService(),
      bubbleValidationService,
      new BubbleStateService(),
      new MenuAnimationService(),
      menuFactory,
      new WindowRuntimeService(),
      new GlobalMouseHook());
  }

  public MainWindow(MainWindowDependencies deps)
    : this(
      deps.HotkeyManager,
      deps.BubbleRepository,
      deps.GestureService,
      deps.BubbleViewFactory,
      deps.BubbleLayoutService,
      deps.BubbleInteractionService,
      deps.BubbleValidationService,
      deps.BubbleStateService,
      deps.MenuAnimationService,
      deps.MenuFactory,
      deps.WindowRuntimeService,
      deps.GlobalMouseHook) {
  }

  public MainWindow(
    IHotkeyManager hotkeyManager,
    IBubbleRepository bubbleRepository,
    GestureService gestureService,
    BubbleViewFactory bubbleViewFactory,
    BubbleLayoutService bubbleLayoutService,
    BubbleInteractionService bubbleInteractionService,
    BubbleValidationService bubbleValidationService,
    BubbleStateService bubbleStateService,
    MenuAnimationService menuAnimationService,
    MenuFactory menuFactory,
    WindowRuntimeService windowRuntimeService,
    IGlobalMouseHook globalHook) {
    _hotkeyManager = hotkeyManager ?? throw new ArgumentNullException(nameof(hotkeyManager));
    _bubbleRepository = bubbleRepository ?? throw new ArgumentNullException(nameof(bubbleRepository));
    _gestureService = gestureService ?? throw new ArgumentNullException(nameof(gestureService));
    _bubbleViewFactory = bubbleViewFactory ?? throw new ArgumentNullException(nameof(bubbleViewFactory));
    _bubbleLayoutService = bubbleLayoutService ?? throw new ArgumentNullException(nameof(bubbleLayoutService));
    _bubbleInteractionService = bubbleInteractionService ?? throw new ArgumentNullException(nameof(bubbleInteractionService));
    _bubbleValidationService = bubbleValidationService ?? throw new ArgumentNullException(nameof(bubbleValidationService));
    _bubbleStateService = bubbleStateService ?? throw new ArgumentNullException(nameof(bubbleStateService));
    _menuAnimationService = menuAnimationService ?? throw new ArgumentNullException(nameof(menuAnimationService));
    _menuFactory = menuFactory ?? throw new ArgumentNullException(nameof(menuFactory));
    _windowRuntimeService = windowRuntimeService ?? throw new ArgumentNullException(nameof(windowRuntimeService));
    _globalHook = globalHook ?? throw new ArgumentNullException(nameof(globalHook));

    InitializeComponent();
    // 使用預設抗鋸齒，避免邊緣與文字在透明視窗中過度粗糙
    this.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Unspecified);

    // 關鍵：使用 VirtualScreen 涵蓋所有螢幕
    this.Left = SystemParameters.VirtualScreenLeft;
    this.Top = SystemParameters.VirtualScreenTop;
    this.Width = SystemParameters.VirtualScreenWidth;
    this.Height = SystemParameters.VirtualScreenHeight;

    // 讓 Canvas 大小跟隨視窗
    MainCanvas.Width = this.Width;
    MainCanvas.Height = this.Height;
    // 3. 確保背景是透明但「存在」的，這樣才抓得到全域事件
    MainCanvas.Background = System.Windows.Media.Brushes.Transparent;

    _bubbleStateService.Initialize(_bubbleRepository.LoadAll()); // 程式啟動先載入
    ApplyQualityMode(_qualityMode);
    // 監聽中心圓的右鍵
    CenterHub.MouseRightButtonUp += CenterHub_MouseRightButtonUp;

    // 初始化時先給 CenterHub 一個畫面中間的位置，避免一開始是 NaN
    //Canvas.SetLeft(CenterHub, this.Width / 2 - 50);
    //Canvas.SetTop(CenterHub, this.Height / 2 - 50);

    RefreshLayout();

    _globalHook.MouseMoved += (x, y) => {
      QueueMouseMoveForGesture(x, y);
    };

    if (_gestureEnabled) {
      _globalHook.Install();
    }
  }

  // 記得在程式關閉時卸載，否則系統會變慢
  protected override void OnClosed(EventArgs e) {
    _globalHook.Uninstall();
    _hotkeyManager.Unregister();
    base.OnClosed(e);
  }

  private void CenterHub_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
    var menu = _menuFactory.CreateCenterHubMenu(() => Application.Current.Shutdown());
    var gestureItem = new MenuItem { Header = "啟用手勢", IsCheckable = true, IsChecked = _gestureEnabled };
    gestureItem.Click += (_, _) => SetGestureEnabled(gestureItem.IsChecked);
    var qualityRoot = new MenuItem { Header = "畫質模式" };
    qualityRoot.Items.Add(CreateQualityMenuItem("漂亮 (Pretty)", UiQualityMode.Pretty));
    qualityRoot.Items.Add(CreateQualityMenuItem("平衡 (Balanced)", UiQualityMode.Balanced));
    qualityRoot.Items.Add(CreateQualityMenuItem("效能 (Performance)", UiQualityMode.Performance));
    var cleanupInvalidItem = new MenuItem { Header = "清理失效連結" };
    cleanupInvalidItem.Click += (_, _) => CleanupInvalidLinks();
    menu.Items.Insert(0, gestureItem);
    menu.Items.Insert(1, qualityRoot);
    menu.Items.Insert(2, cleanupInvalidItem);
    menu.Items.Insert(3, new Separator());
    menu.IsOpen = true;
  }

  private void CleanupInvalidLinks() {
    int removed = _bubbleValidationService.RemoveInvalidLinks(_bubbleStateService.AllBubbles);
    if (removed <= 0) {
      MessageBox.Show("未發現失效連結。");
      return;
    }

    _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    RefreshLayout();
    MessageBox.Show($"已清理 {removed} 個失效連結。");
  }

  private MenuItem CreateQualityMenuItem(string text, UiQualityMode mode) {
    var item = new MenuItem { Header = text, IsCheckable = true, IsChecked = _qualityMode == mode };
    item.Click += (_, _) => ApplyQualityMode(mode);
    return item;
  }

  private void ApplyQualityMode(UiQualityMode mode) {
    _qualityMode = mode;
    _bubbleViewFactory.QualityMode = mode;

    foreach (var bubble in AnimationWrapper.Children.OfType<BubbleControl>()) {
      bubble.QualityMode = mode;
    }

    switch (mode) {
      case UiQualityMode.Pretty:
        HubGlowEffect.BlurRadius = 22;
        HubGlowEffect.Opacity = 0.78;
        HubBody.Opacity = 0.84;
        HubHighlight.Opacity = 0.88;
        HubHighlightBlur.Radius = 8;
        break;
      case UiQualityMode.Performance:
        HubGlowEffect.BlurRadius = 10;
        HubGlowEffect.Opacity = 0.38;
        HubBody.Opacity = 0.68;
        HubHighlight.Opacity = 0.48;
        HubHighlightBlur.Radius = 3;
        break;
      default:
        HubGlowEffect.BlurRadius = 16;
        HubGlowEffect.Opacity = 0.62;
        HubBody.Opacity = 0.74;
        HubHighlight.Opacity = 0.68;
        HubHighlightBlur.Radius = 7;
        break;
    }
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
    _windowRuntimeService.EnsureTopMost(this);
  }

  protected override void OnActivated(EventArgs e) {
    base.OnActivated(e);
    if (this.Visibility == Visibility.Visible) {
      _windowRuntimeService.EnsureTopMost(this);
    }
  }

  /// <summary>
  /// 拖曳事件初始化
  /// </summary>
  private void InitializeDragEvents() {
    this.MouseMove += Window_MouseMove;
    this.MouseLeftButtonUp += Window_MouseLeftButtonUp;
  }

  private void CenterHub_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

    // 如果目前在子層級，且只是輕點一下 (ClickCount == 1)
    if (!_bubbleStateService.IsAtRoot && e.ClickCount == 1) {
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
      _bubbleInteractionService.MoveElement(_draggedElement, _clickPosition, currentPos);
      _clickPosition = currentPos;
      return;
    }

    // --- 新增：手勢偵測邏輯 (當視窗內容隱藏時) ---
    // 選單開啟時，利用原本的 MouseMove 偵測「逆時針關閉」
    // --- 修正：當選單開啟時的手勢偵測 ---
    //if (this.Visibility == Visibility.Visible) {
    //  System.Drawing.Point p;
    //  if (Windows.Win32.PInvoke.GetCursorPos(out p)) {
    //    var source = PresentationSource.FromVisual(this);
    //    if (source?.CompositionTarget != null) {
    //      var m = source.CompositionTarget.TransformFromDevice;
    //      // 轉換成 DIP 座標，這樣才跟 GlobalHook 傳入的尺度一致
    //      var dipPos = m.Transform(new System.Windows.Point(p.X, p.Y));
    //      DetectCircleGesture(dipPos);
    //    }
    //  }
    //}
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
        if (!_bubbleStateService.IsAtRoot && TryMoveBubbleToParent(_draggedElement)) {
          SetBubblesOpacity(1);
          _draggedElement = null;
          return;
        }

        // 泡泡拖完，檢查合併或回彈
        bool isMerged = CheckForMerger(_draggedElement);
        if (!isMerged) {
          if (!TryReorderBubble(_draggedElement)) {
            ReturnBubbleToOrbit(_draggedElement);
          }
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

    int index = _bubbleStateService.CurrentViewBubbles.IndexOf(data);
    int total = _bubbleStateService.CurrentViewBubbles.Count;
    var target = _bubbleLayoutService.CalculateBubblePosition(index, total, element.ActualWidth, element.ActualHeight, GetOrbitCenter());
    double targetX = target.X;
    double targetY = target.Y;

    DoubleAnimation animX = new DoubleAnimation(Canvas.GetLeft(element), targetX, TimeSpan.FromSeconds(0.4)) {
      EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
    };
    DoubleAnimation animY = new DoubleAnimation(Canvas.GetTop(element), targetY, TimeSpan.FromSeconds(0.4)) {
      EasingFunction = new BackEase { Amplitude = 0.3, EasingMode = EasingMode.EaseOut }
    };

    animX.Completed += (s, e) => { element.BeginAnimation(Canvas.LeftProperty, null); Canvas.SetLeft(element, targetX); };
    animY.Completed += (s, e) => { element.BeginAnimation(Canvas.TopProperty, null); Canvas.SetTop(element, targetY); };

    element.BeginAnimation(Canvas.LeftProperty, animX);
    element.BeginAnimation(Canvas.TopProperty, animY);
  }

  private void SetBubblesOpacity(double opacity) {
    // 改成抓 AnimationWrapper 的子元素
    var bubbles = AnimationWrapper.Children.OfType<UIElement>()
                                           .Where(x => x != CenterHub)
                                           .ToList();
    foreach (var el in bubbles) {
      el.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromSeconds(0.2)));
    }
  }


  private void ExecuteBubbleAction(FrameworkElement element) {
    var data = element.Tag as BubbleItem;
    if (data == null || string.IsNullOrEmpty(data.Path)) return;

    try {
      // 如果是集合，則執行展開邏輯（我們之前討論過的切換層級）
      // 關鍵：如果 SubItems 有東西，或者是集合路徑
      if (data.SubItems.Count > 0 || data.Path == BubbleConstants.CollectionPath) {
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
    _bubbleStateService.ExpandCollection(collection);

    // 4. 更新中間 Menu 顯示名稱
    UpdateCenterHubText(collection.Name);

    // 3. 更新 UI
    RefreshLayout();
  }

  private void UpdateCenterHubText(string text) {
    // 進入集合時切換為金色中心球
    ApplyCenterHubAccent(Colors.Gold);
    HubText.Text = text;
  }

  private void ApplyCenterHubAccent(Color accent) {
    HubGlowEffect.Color = accent;
    HubBodyMiddleStop.Color = Color.FromArgb(170, accent.R, accent.G, accent.B);
    HubBodyEdgeStop.Color = Color.FromArgb(122,
      (byte)(accent.R * 0.35),
      (byte)(accent.G * 0.35),
      (byte)(accent.B * 0.35));
  }

  private nint HwndHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) {
    if (_windowRuntimeService.IsHotkeyMessage(msg, wParam, _hotkeyManager.HotkeyId)) {
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
      _bubbleStateService.ResetToRoot();

      // 修正這裡：控制內部的 Ellipse 物件
      ApplyCenterHubAccent(Colors.Cyan);
      HubText.Text = BubbleConstants.RootHubText;

      // 3. 執行「噴射彈出」動畫
      ShowMenuWithAnimation();

      this.Activate(); // 確保獲取焦點
    }
  }

  public bool IsMenuVisible => this.Visibility == Visibility.Visible;
  public bool IsGestureEnabled => _gestureEnabled;

  public void SetGestureEnabled(bool enabled) {
    if (_gestureEnabled == enabled) return;
    _gestureEnabled = enabled;
    if (_gestureEnabled) {
      _globalHook.Install();
    } else {
      ResetGesture();
      _globalHook.Uninstall();
    }
  }

  public void ToggleMenuFromTray() {
    OnHotkeyTriggered();
  }

  private void UpdatePositionToMouse() {
    var orbitCenter = GetOrbitCenter();
    if (_windowRuntimeService.TryUpdateWrapperToMouse(this, AnimationWrapper, orbitCenter.X, orbitCenter.Y)) {
      // 強制 UI 刷新位置，避免動畫抓到舊座標
      this.UpdateLayout();
    }
  }

  private void HideMenuWithAnimation() {
    _menuAnimationService.PlayHide(MainCanvas, MainScale, MainRotate, () => {
      // 不要設為 Collapsed，否則滑鼠手勢會失效
      this.Visibility = Visibility.Collapsed; // 先徹底隱藏
      MainCanvas.Opacity = 0; // 隱藏後立刻歸零，為下次開啟做準備

      // 【重要】解除動畫對屬性的鎖定，否則下次 UpdatePositionToMouse 修改位置會無效
      MainScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
      MainScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
      MainRotate.BeginAnimation(RotateTransform.AngleProperty, null);

      ClearBubbles();
      ResetGesture(); // 清空手勢殘留
    });
  }

  private void ClearBubbles() {
    // 改為清理 AnimationWrapper，但避開 CenterHub
    var toRemove = AnimationWrapper.Children.OfType<FrameworkElement>()
                                         .Where(x => x != CenterHub)
                                         .ToList();
    foreach (var item in toRemove) AnimationWrapper.Children.Remove(item);
  }

  private async void ShowMenuWithAnimation() {
    // 1. 初始化顯示狀態
    _menuAnimationService.PrepareShowState(this, MainCanvas, MainScale, MainRotate);

    // 2. 核心定位：讓舞台中心對準滑鼠
    // 這裡調用 UpdatePositionToMouse，它會執行 Canvas.SetLeft(AnimationWrapper, mouseX - 400)
    UpdatePositionToMouse();

    // 3. 確保佈局已計算（重要：否則 GetPosition 可能會拿到舊資料）
    this.UpdateLayout();
    await this.NextFrame(); // 等待下一影格確保渲染引擎抓到新位置

    // 4. 重置手勢快照，避免舊的滑鼠軌跡干擾
    ResetGesture();

    // 5. 同步執行重新佈局 (生成泡泡)
    RefreshLayout();

    // 6. 啟動動畫
    _menuAnimationService.PlayShow(MainCanvas, MainScale, MainRotate);

    // 確保視窗獲取焦點
    this.Activate();
    _windowRuntimeService.EnsureTopMost(this);
  }

  private void QueueMouseMoveForGesture(double x, double y) {
    lock (_mouseMoveSync) {
      _queuedMousePoint = new System.Windows.Point(x, y);
      _hasQueuedMousePoint = true;
      if (_mouseMoveDispatchScheduled) return;
      _mouseMoveDispatchScheduled = true;
    }

    // 合併高頻滑鼠事件，避免 Dispatcher 被大量 BeginInvoke 壓爆
    Dispatcher.BeginInvoke(DispatcherPriority.Background, ProcessQueuedMouseMoves);
  }

  private void ProcessQueuedMouseMoves() {
    while (true) {
      System.Windows.Point point;
      lock (_mouseMoveSync) {
        if (!_hasQueuedMousePoint) {
          _mouseMoveDispatchScheduled = false;
          return;
        }

        point = _queuedMousePoint;
        _hasQueuedMousePoint = false;
      }

      if (_gestureEnabled) {
        // 不管視窗是 Visible 還是 Collapsed，都由 GlobalHook 驅動偵測
        // 這樣就不會因為滑鼠「離清單太遠」而收不到事件
        DetectCircleGesture(point);
      }
    }
  }


  private bool CheckForMerger(UIElement draggedBubble) {
    // 改從 AnimationWrapper 找，而不是 MainCanvas
    var allItems = AnimationWrapper.Children.OfType<FrameworkElement>().ToList();
    var target = _bubbleInteractionService.FindMergeTarget(allItems, draggedBubble, CenterHub, 50);
    if (target == null) return false;

    return MergeBubbles(target, draggedBubble);
  }

  private bool TryMoveBubbleToParent(FrameworkElement element) {
    if (element.Tag is not BubbleItem data) return false;

    // 拖曳泡泡碰到中心圈時，將該泡泡提升到上一層
    if (_bubbleInteractionService.GetDistance(element, CenterHub) > 74) {
      return false;
    }

    if (!_bubbleStateService.MoveCurrentBubbleToParent(data)) {
      return false;
    }

    RefreshLayout();
    _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    return true;
  }

  private bool TryReorderBubble(FrameworkElement element) {
    if (element.Tag is not BubbleItem data) return false;

    int total = _bubbleStateService.CurrentViewBubbles.Count;
    if (total <= 1) return false;

    double left = Canvas.GetLeft(element);
    double top = Canvas.GetTop(element);
    double width = element.ActualWidth > 0 ? element.ActualWidth : element.Width;
    double height = element.ActualHeight > 0 ? element.ActualHeight : element.Height;
    var bubbleCenter = new Point(left + (width / 2), top + (height / 2));
    var orbitCenter = GetOrbitCenter();

    // 靠近中心代表上一層手勢，避免跟重排判定打架
    var dxToCenter = bubbleCenter.X - orbitCenter.X;
    var dyToCenter = bubbleCenter.Y - orbitCenter.Y;
    if (Math.Sqrt((dxToCenter * dxToCenter) + (dyToCenter * dyToCenter)) < 90) {
      return false;
    }

    double angle = Math.Atan2(dyToCenter, dxToCenter);
    if (angle < 0) angle += Math.PI * 2;
    double step = (Math.PI * 2) / total;
    int targetIndex = (int)Math.Round(angle / step, MidpointRounding.AwayFromZero) % total;

    if (!_bubbleStateService.ReorderInCurrentView(data, targetIndex)) {
      return false;
    }

    RefreshLayout();
    _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    return true;
  }

  private bool MergeBubbles(UIElement target, UIElement source) {
    var targetFE = target as FrameworkElement;
    var sourceFE = source as FrameworkElement;
    if (targetFE == null || sourceFE == null) return false;

    var targetData = targetFE.Tag as BubbleItem;
    var sourceData = sourceFE.Tag as BubbleItem;
    if (targetData == null || sourceData == null) return false;

    var mergeMode = ResolveCollectionMergeMode(targetData, sourceData);
    if (mergeMode == null) return false;

    // 1. 建立新的集合物件
    var collectionData = _bubbleInteractionService.CreateMergedCollection(targetData, sourceData, mergeMode.Value);

    // 3. 重要：從「當前顯示清單」中移除這兩個，並換成新的集合
    // 這樣其他的泡泡就會被保留在 _currentViewBubbles 中
    _bubbleStateService.ApplyMerge(targetData, sourceData, collectionData);

    // 5. 重新佈局 (這會根據更新後的 _currentViewBubbles 畫出所有剩下的泡泡)
    RefreshLayout();

    PlayMergeEffect(CenterHub);

    _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    return true;
  }

  private CollectionMergeMode? ResolveCollectionMergeMode(BubbleItem targetData, BubbleItem sourceData) {
    // 只在「拖曳中的泡泡是集合」時才詢問模式；
    // 一般泡泡拖進集合時直接用預設合併，不彈窗。
    if (!IsCollectionBubble(sourceData)) {
      return CollectionMergeMode.FlattenItems;
    }

    var dialog = new CollectionMergeModeDialog {
      Owner = this
    };

    var dialogPos = GetCursorDipPosition();
    dialog.Left = dialogPos.X + 12;
    dialog.Top = dialogPos.Y + 12;

    var ok = dialog.ShowDialog();
    if (ok == true) {
      return dialog.SelectedMode;
    }
    return null;
  }

  private static bool IsCollectionBubble(BubbleItem item) {
    return item.SubItems.Count > 0 || item.Path == BubbleConstants.CollectionPath;
  }

  private Point GetCursorDipPosition() {
    if (Windows.Win32.PInvoke.GetCursorPos(out var p)) {
      var source = PresentationSource.FromVisual(this);
      if (source?.CompositionTarget != null) {
        var m = source.CompositionTarget.TransformFromDevice;
        return m.Transform(new Point(p.X, p.Y));
      }
    }
    return new Point(SystemParameters.WorkArea.Left + 80, SystemParameters.WorkArea.Top + 80);
  }

  private void RefreshLayout() {

    ClearBubbles(); // 清空畫面

    // 這裡改用非同步載入，避免一次擠爆 UI 執行緒
    for (int i = 0; i < _bubbleStateService.CurrentViewBubbles.Count; i++) {
      AddBubble(_bubbleStateService.CurrentViewBubbles[i], i, _bubbleStateService.CurrentViewBubbles.Count);
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
    var bubble = _bubbleViewFactory.CreateBubble(data, Bubble_MouseLeftButtonDown, OnBubbleDeleteRequested, OnBubbleRenameRequested);
    AnimationWrapper.Children.Add(bubble);

    // 由佈局服務統一計算座標，讓 MainWindow 只負責呈現
    var position = _bubbleLayoutService.CalculateBubblePosition(index, totalCount, bubble.Width, bubble.Height, GetOrbitCenter());
    double x = position.X;
    double y = position.Y;

    Canvas.SetLeft(bubble, x);
    Canvas.SetTop(bubble, y);

    // 噴射動畫：從中心(1,1)噴射到正確位置的 Scale(1,1)
    // 注意：這裡我們讓 RenderTransformOrigin 為 0.5,0.5，所以動畫看起來是原地變大
    DoubleAnimation expand = new DoubleAnimation {
      To = 1.0,
      Duration = TimeSpan.FromSeconds(0.3),
      BeginTime = TimeSpan.FromSeconds(index * 0.05),
      EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
    };
    bubble.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, expand);
    bubble.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, expand);
  }

  private void OnBubbleDeleteRequested(BubbleItem data) {
    if (_bubbleStateService.RemoveBubble(data)) {
      RefreshLayout();
      _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    }
  }

  private void OnBubbleRenameRequested(BubbleItem data) {
    var currentName = data.Name;
    var newName = Interaction.InputBox("請輸入新名稱：", "更改名稱", currentName);

    if (string.IsNullOrWhiteSpace(newName)) {
      return;
    }

    newName = newName.Trim();
    if (string.Equals(newName, currentName, StringComparison.Ordinal)) {
      return;
    }

    data.Name = newName;
    RefreshLayout();
    _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
  }

  private Point GetOrbitCenter() {
    double left = Canvas.GetLeft(CenterHub);
    double top = Canvas.GetTop(CenterHub);

    if (double.IsNaN(left)) left = 350;
    if (double.IsNaN(top)) top = 350;

    double width = CenterHub.ActualWidth > 0 ? CenterHub.ActualWidth : CenterHub.Width;
    double height = CenterHub.ActualHeight > 0 ? CenterHub.ActualHeight : CenterHub.Height;
    return new Point(left + (width / 2), top + (height / 2));
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

      // 交由狀態服務處理去重與資料更新
      var newFiles = _bubbleStateService.AddFiles(files);

      if (newFiles.Count == 0) return; // 如果全部都重複，就不執行

      RefreshLayout();
      _bubbleRepository.SaveAll(_bubbleStateService.AllBubbles);
    }
  }

  private void BackToParent() {
    if (!_bubbleStateService.TryBackToParent()) {
      return;
    }

    if (_bubbleStateService.IsAtRoot) {
      ApplyCenterHubAccent(Colors.Cyan);
      HubText.Text = BubbleConstants.RootHubText;
    } else {
      var parent = _bubbleStateService.CurrentNavigatedCollection;
      if (parent != null) {
        UpdateCenterHubText(parent.Name);
      }
    }

    RefreshLayout();
  }

  private void DetectCircleGesture(System.Windows.Point currentPos) {
    var trigger = _gestureService.ProcessPoint(currentPos, this.Visibility == Visibility.Visible);
    if (trigger == GestureTrigger.OpenMenu) {
      OnHotkeyTriggered();
    } else if (trigger == GestureTrigger.CloseMenu) {
      HideMenuWithAnimation();
    }
  }

  private void ResetGesture() {
    _gestureService.Reset();
  }

}