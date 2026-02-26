using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Icons;
using OrbitBubble.Domain.Bubbles;
using OrbitBubble.Domain.Launch;

namespace OrbitBubble.ViewModels;

public partial class MainViewModel : ObservableObject {
  private readonly ILogger<MainViewModel> _logger;
  private readonly IBubbleService _bubbleService;
  private readonly IIconProvider _iconProvider;
  private readonly ILauncherService _launcher;

  public System.Collections.ObjectModel.ObservableCollection<BubbleViewModel> VisibleBubbles { get; } = new();

  [ObservableProperty] private bool isMenuOpen;

  // Overlay / layout
  public double OverlaySize { get; } = 520;
  public double HubSize { get; } = 72;
  public double BubbleSize { get; } = 72;

  public double BubbleCorner => BubbleSize / 2;
  public double HubCorner => HubSize / 2;

  private double _centerX;
  private double _centerY;

  // Ring
  public double RingRadius { get; } = 180;
  public double RingDiameter => RingRadius * 2;

  public double RingLeft => _centerX - RingRadius;
  public double RingTop => _centerY - RingRadius;

  public double HubLeft => _centerX - HubSize / 2;
  public double HubTop => _centerY - HubSize / 2;

  public double AngularSpeed { get; set; } = 0.012; // 每幀角度增量（可調）
  public double AngleOffset { get; set; } = 0;

  private int _refreshVersion = 0;

  public MainViewModel(ILogger<MainViewModel> logger,
                       IBubbleService bubbleService,
                       IIconProvider iconProvider,
                       ILauncherService launcher) {
    _logger = logger;
    _bubbleService = bubbleService;
    _iconProvider = iconProvider;
    _launcher = launcher;

    // 預設中心在 overlay 正中
    SetCenter(OverlaySize / 2, OverlaySize / 2);
  }

  public void Initialize() {
    var load = _bubbleService.Load();
    if (!load.IsSuccess)
      _logger.LogWarning("Bubble load failed. code={Code} msg={Msg}", load.Error!.Code, load.Error.Message);
  }

  public void SetCenter(double x, double y) {
    _centerX = x;
    _centerY = y;

    OnPropertyChanged(nameof(RingLeft));
    OnPropertyChanged(nameof(RingTop));
    OnPropertyChanged(nameof(HubLeft));
    OnPropertyChanged(nameof(HubTop));
  }

  public bool AddFolder(string path) {
    var r = _bubbleService.AddFolder(path);
    if (!r.IsSuccess) {
      _logger.LogWarning("AddFolder failed. code={Code} msg={Msg}", r.Error!.Code, r.Error.Message);
      return false;
    }
    return true;
  }

  public bool AddFile(string path) {
    var r = _bubbleService.AddFile(path);
    if (!r.IsSuccess) {
      _logger.LogWarning("AddFile failed. code={Code} msg={Msg}", r.Error!.Code, r.Error.Message);
      return false;
    }
    return true;
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

    var launched = _launcher.Launch(item);
    if (!launched.IsSuccess)
      _logger.LogWarning("Launch failed. code={Code} msg={Msg}", launched.Error!.Code, launched.Error.Message);
  }

  public async Task RefreshVisibleBubblesAsync() {
    var version = Interlocked.Increment(ref _refreshVersion);

    VisibleBubbles.Clear();

    var items = _bubbleService.State.GetVisibleItems().ToList();
    if (items.Count == 0) return;

    for (int i = 0; i < items.Count; i++) {
      var it = items[i];
      var vm = new BubbleViewModel(it.Id, it.DisplayName, it.Type, it.Path) {
        Icon = _iconProvider.Placeholder
      };
      VisibleBubbles.Add(vm);

      if (!string.IsNullOrWhiteSpace(it.Path))
        _ = LoadIconAsync(vm, it.Path!, version);
    }

    UpdateBubblePositions();
    await Task.CompletedTask;
  }

  public void UpdateBubblePositions() {
    var count = VisibleBubbles.Count;
    if (count == 0) return;

    // 以中心點+RingRadius 排布，並加 AngleOffset 讓它繞行
    for (int i = 0; i < count; i++) {
      var angle = (Math.PI * 2) * i / count + AngleOffset;

      var x = _centerX + RingRadius * Math.Cos(angle) - BubbleSize / 2;
      var y = _centerY + RingRadius * Math.Sin(angle) - BubbleSize / 2;

      VisibleBubbles[i].X = x;
      VisibleBubbles[i].Y = y;
    }
  }

  private async Task LoadIconAsync(BubbleViewModel vm, string path, int version) {
    try {
      var icon = await _iconProvider.GetIconAsync(path);

      if (version != Volatile.Read(ref _refreshVersion))
        return;

      vm.Icon = icon;
    } catch {
      if (version != Volatile.Read(ref _refreshVersion))
        return;

      vm.Icon = _iconProvider.Placeholder;
    }
  }

  public bool Back() {
    var r = _bubbleService.Back();
    if (!r.IsSuccess) {
      _logger.LogWarning("Back failed. code={Code} msg={Msg}", r.Error!.Code, r.Error.Message);
      return false;
    }
    return r.Value == true;
  }

  public bool Merge(string sourceId, string targetId) {
    var r = _bubbleService.MergeIntoCollection(sourceId, targetId);
    if (!r.IsSuccess) {
      _logger.LogWarning("Merge failed. code={Code} msg={Msg}", r.Error!.Code, r.Error.Message);
      return false;
    }
    return true;
  }
}
