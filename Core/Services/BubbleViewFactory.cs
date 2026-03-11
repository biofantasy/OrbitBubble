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
  public UiQualityMode QualityMode { get; set; } = UiQualityMode.Balanced;

  public BubbleViewFactory(IconCacheService iconCacheService, MenuFactory menuFactory) {
    _iconCacheService = iconCacheService ?? throw new ArgumentNullException(nameof(iconCacheService));
    _menuFactory = menuFactory ?? throw new ArgumentNullException(nameof(menuFactory));
  }

  public FrameworkElement CreateBubble(
    BubbleItem data,
    MouseButtonEventHandler onLeftButtonDown,
    Action<BubbleItem> onDeleteRequested) {
    var bubble = new BubbleControl {
      Width = 75,
      Height = 75,
      BubbleSize = 75,
      RenderTransformOrigin = new Point(0.5, 0.5),
      RenderTransform = new ScaleTransform(0, 0),
      IconSource = _iconCacheService.GetIcon(data),
      Label = data.Name,
      AccentColor = (data.SubItems.Count > 0 || data.Path == BubbleConstants.CollectionPath) ? Colors.Gold : Colors.Cyan,
      QualityMode = QualityMode,
      Tag = data
    };

    bubble.BubbleMouseRightButtonUp += (s, e) => {
      var menu = _menuFactory.CreateBubbleMenu(() => onDeleteRequested(data));
      menu.IsOpen = true;
      e.Handled = true;
    };

    bubble.BubbleMouseLeftButtonDown += onLeftButtonDown;
    return bubble;
  }
}
