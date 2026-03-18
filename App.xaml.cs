using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Managers;
using OrbitBubble.Core.Repositories;
using OrbitBubble.Core.Services;
using System.Threading;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OrbitBubble;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
  private const string SingleInstanceMutexName = "OrbitBubble.SingleInstance";
  private Mutex? _singleInstanceMutex;
  private MainWindow? _mainWindow;
  private Forms.NotifyIcon? _notifyIcon;
  private Forms.ToolStripMenuItem? _toggleMenuItem;
  private Forms.ToolStripMenuItem? _gestureToggleMenuItem;

  protected override void OnStartup(StartupEventArgs e) {
    _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
    if (!createdNew) {
      _singleInstanceMutex.Dispose();
      _singleInstanceMutex = null;
      Shutdown();
      return;
    }

    base.OnStartup(e);

    // Composition root：集中組裝可替換元件，避免 MainWindow 直接硬編碼依賴
    var iconCache = new IconCacheService();
    var menuFactory = new MenuFactory();
    var bubbleValidationService = new BubbleValidationService();
    _mainWindow = new MainWindow(
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
    _mainWindow.Show();

    InitializeTrayIcon();
  }

  private void InitializeTrayIcon() {
    _toggleMenuItem = new Forms.ToolStripMenuItem("顯示");
    _toggleMenuItem.Click += (_, _) => ToggleMenuFromTray();

    _gestureToggleMenuItem = new Forms.ToolStripMenuItem("啟用手勢") { CheckOnClick = true };
    _gestureToggleMenuItem.Click += (_, _) => ToggleGestureFromTray();

    var exitMenuItem = new Forms.ToolStripMenuItem("結束程式");
    exitMenuItem.Click += (_, _) => Current.Shutdown();

    var contextMenu = new Forms.ContextMenuStrip();
    contextMenu.Opening += (_, _) => RefreshTrayMenuState();
    contextMenu.Items.Add(_toggleMenuItem);
    contextMenu.Items.Add(_gestureToggleMenuItem);
    contextMenu.Items.Add(new Forms.ToolStripSeparator());
    contextMenu.Items.Add(exitMenuItem);

    _notifyIcon = new Forms.NotifyIcon {
      Text = "OrbitBubble",
      Icon = System.Drawing.SystemIcons.Application,
      Visible = true,
      ContextMenuStrip = contextMenu
    };
    _notifyIcon.DoubleClick += (_, _) => ToggleMenuFromTray();
  }

  private void ToggleMenuFromTray() {
    if (_mainWindow == null) return;
    _mainWindow.Dispatcher.Invoke(() => _mainWindow.ToggleMenuFromTray());
    RefreshTrayMenuState();
  }

  private void ToggleGestureFromTray() {
    if (_mainWindow == null || _gestureToggleMenuItem == null) return;
    _mainWindow.Dispatcher.Invoke(() => _mainWindow.SetGestureEnabled(_gestureToggleMenuItem.Checked));
    RefreshTrayMenuState();
  }

  private void RefreshTrayMenuState() {
    if (_toggleMenuItem == null || _mainWindow == null) return;
    _toggleMenuItem.Text = _mainWindow.IsMenuVisible ? "隱藏" : "顯示";
    if (_gestureToggleMenuItem != null) {
      _gestureToggleMenuItem.Checked = _mainWindow.IsGestureEnabled;
    }
  }

  protected override void OnExit(ExitEventArgs e) {
    if (_notifyIcon != null) {
      _notifyIcon.Visible = false;
      _notifyIcon.Dispose();
      _notifyIcon = null;
    }

    if (_singleInstanceMutex != null) {
      _singleInstanceMutex.ReleaseMutex();
      _singleInstanceMutex.Dispose();
      _singleInstanceMutex = null;
    }

    base.OnExit(e);
  }
}
