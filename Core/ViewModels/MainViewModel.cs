using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Icons;
using OrbitBubble.Domain.Bubbles;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OrbitBubble.Core.ViewModels;

public partial class MainViewModel : ObservableObject {
  private readonly ILogger<MainViewModel> _logger;
  private readonly IBubbleService _bubbleService;
  private readonly IIconProvider _iconProvider;

  public ObservableCollection<BubbleViewModel> VisibleBubbles { get; } = new();

  [ObservableProperty] private bool isMenuOpen;

  public MainViewModel(
      ILogger<MainViewModel> logger,
      IBubbleService bubbleService,
      IIconProvider iconProvider) {
    _logger = logger;
    _bubbleService = bubbleService;
    _iconProvider = iconProvider;
  }

  public void Initialize() {
    var load = _bubbleService.Load();
    if (!load.IsSuccess) {
      _logger.LogError(load.Error!.Exception, "Bubble load failed. code={Code} msg={Msg}",
          load.Error.Code, load.Error.Message);
    }

    RefreshVisibleBubblesAsync().ConfigureAwait(false);
  }

  [RelayCommand]
  private void ToggleMenu() {
    IsMenuOpen = !IsMenuOpen;
    _logger.LogInformation("Menu toggled. open={Open}", IsMenuOpen);
  }

  [RelayCommand]
  private void Back() {
    var r = _bubbleService.Back();
    if (!r.IsSuccess)
      _logger.LogWarning("Back failed. code={Code} msg={Msg}", r.Error!.Code, r.Error.Message);

    _ = RefreshVisibleBubblesAsync();
  }

  [RelayCommand]
  private void OpenBubble(string id) {
    var item = _bubbleService.State.GetVisibleItems().FirstOrDefault(x => x.Id == id);
    if (item == null) return;

    if (item.Type == BubbleItemType.Collection) {
      var nav = _bubbleService.EnterCollection(id);
      if (!nav.IsSuccess)
        _logger.LogWarning("Enter collection failed. code={Code} msg={Msg}", nav.Error!.Code, nav.Error.Message);

      _ = RefreshVisibleBubblesAsync();
      return;
    }

    // File/Folder：你原本的開啟行為可以放到一個 ILauncherService（之後 Phase 5.5）
    // 先留 stub：不要讓 VM 直接 Process.Start（但現在先不做也行）
    _logger.LogInformation("Open item requested. type={Type} path={Path}", item.Type, item.Path);
  }

  // MainViewModel 內
  private double _viewportW = 900;
  private double _viewportH = 700;

  // 每次 refresh 都 ++，讓舊的 icon 任務回來時不會污染新的畫面
  private int _refreshVersion = 0;

  public async Task RefreshVisibleBubblesAsync() {
    // 取得這次 refresh 的版本號
    var version = Interlocked.Increment(ref _refreshVersion);

    try {
      // 1) 取目前層級 items（root 或 collection）
      var items = _bubbleService.State.GetVisibleItems().ToList();

      // 2) 先清空 UI collection
      // ObservableCollection 必須在 UI thread 操作，
      // 但你通常從 UI thread 呼叫 Refresh；保守起見用 Dispatcher（如果你沒有 dispatcher，就直接 Clear 也行）
      VisibleBubbles.Clear();

      if (items.Count == 0)
        return;

      // 3) 計算圓形佈局（中心用 viewport）
      // 泡泡寬高固定 80，所以中心點要減 40 讓泡泡中心在畫面中心
      const double bubbleSize = 80;
      var centerX = _viewportW / 2 - bubbleSize / 2;
      var centerY = _viewportH / 2 - bubbleSize / 2;

      // radius 可依數量調整（太多就放大）
      var radius = ComputeRadius(items.Count);

      var positions = SimpleCircleLayout(items.Count, radius, centerX, centerY);

      // 4) 建 VM + placeholder icon
      for (int i = 0; i < items.Count; i++) {
        var it = items[i];
        var vm = new BubbleViewModel(it.Id, it.DisplayName, it.Type, it.Path) {
          X = positions[i].x,
          Y = positions[i].y,
          Icon = _iconProvider.Placeholder
        };

        VisibleBubbles.Add(vm);

        // 5) icon 非同步載入（只針對有 path 的）
        if (!string.IsNullOrWhiteSpace(it.Path)) {
          _ = LoadIconAsync(vm, it.Path!, version);
        }
      }

      await Task.CompletedTask;
    } catch (Exception ex) {
      _logger.LogError(ex, "RefreshVisibleBubblesAsync failed.");
    }
  }

  /// <summary>
  /// 根據泡泡數量調 radius：
  /// 少量固定 180；多了逐步放大，避免擠成一團
  /// </summary>
  private static double ComputeRadius(int count) {
    if (count <= 6) return 180;
    if (count <= 10) return 220;
    if (count <= 14) return 260;
    if (count <= 20) return 320;
    return 380;
  }

  /// <summary>
  /// 防呆：如果 refreshVersion 已經變了，代表畫面已刷新/換頁，舊任務不要再更新 icon
  /// </summary>
  private async Task LoadIconAsync(BubbleViewModel vm, string path, int version) {
    try {
      var icon = await _iconProvider.GetIconAsync(path);

      // ✅ 防止舊 refresh 任務回來亂改新的畫面
      if (version != Volatile.Read(ref _refreshVersion))
        return;

      vm.Icon = icon;
    } catch (Exception ex) {
      _logger.LogWarning(ex, "Icon load failed. path={Path}", path);

      if (version != Volatile.Read(ref _refreshVersion))
        return;

      vm.Icon = _iconProvider.Placeholder;
    }
  }

  /// <summary>
  /// 簡單圓形佈局：回傳每個泡泡的 Canvas.Left/Top
  /// </summary>
  private static (double x, double y)[] SimpleCircleLayout(int count, double radius, double centerX, double centerY) {
    var result = new (double x, double y)[count];
    if (count <= 0) return result;

    for (int i = 0; i < count; i++) {
      var angle = (Math.PI * 2) * i / count;
      var x = centerX + radius * Math.Cos(angle);
      var y = centerY + radius * Math.Sin(angle);
      result[i] = (x, y);
    }

    return result;
  }

  public void UpdateViewport(double width, double height) {
    if (width <= 0 || height <= 0) return;
    _viewportW = width;
    _viewportH = height;

    // 重新排版
    _ = RefreshVisibleBubblesAsync();
  }
}