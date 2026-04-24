using OrbitBubble.Core.Models;
using OrbitBubble.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OrbitBubble.Core.Services;

public class BubbleViewFactory {
  private readonly IconCacheService _iconCacheService;
  private readonly MenuFactory _menuFactory;
  private readonly BubbleValidationService _bubbleValidationService;
  public UiQualityMode QualityMode { get; set; } = UiQualityMode.Balanced;
  public IconCacheService IconCache => _iconCacheService;

  public BubbleViewFactory(IconCacheService iconCacheService, MenuFactory menuFactory, BubbleValidationService bubbleValidationService) {
    _iconCacheService = iconCacheService ?? throw new ArgumentNullException(nameof(iconCacheService));
    _menuFactory = menuFactory ?? throw new ArgumentNullException(nameof(menuFactory));
    _bubbleValidationService = bubbleValidationService ?? throw new ArgumentNullException(nameof(bubbleValidationService));
  }

  public BubbleControl CreateBubble(
    BubbleItem data,
    MouseButtonEventHandler onLeftButtonDown,
    Action<BubbleItem> onDeleteRequested,
    Action<BubbleItem> onRenameRequested) {
    bool isCollection = data.SubItems.Count > 0 || data.Path == BubbleConstants.CollectionPath;
    bool isAvailable = _bubbleValidationService.IsAvailable(data);
    var bubble = new BubbleControl {
      Width = 88,
      Height = 88,
      BubbleSize = 88,
      RenderTransformOrigin = new Point(0.5, 0.5),
      RenderTransform = new ScaleTransform(0, 0),
      IconSource = null, // icon 由呼叫端非同步載入，避免在 UI thread 阻塞
      Label = data.Name,
      AccentColor = isCollection ? Colors.Gold : Colors.Cyan,
      QualityMode = QualityMode,
      IsAvailable = isAvailable,
      Tag = data
    };

    bubble.BubbleMouseRightButtonUp += (s, e) => {
      var menu = _menuFactory.CreateBubbleMenu(
        () => onDeleteRequested(data),
        isCollection ? () => onRenameRequested(data) : null,
        !isCollection && !isAvailable ? () => onDeleteRequested(data) : null);
      menu.IsOpen = true;
      e.Handled = true;
    };

    bubble.BubbleMouseLeftButtonDown += onLeftButtonDown;
    return bubble;
  }
}
