using OrbitBubble.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace OrbitBubble.Core.Services;

public class BubbleViewFactory {
  private readonly IconCacheService _iconCacheService;
  private readonly MenuFactory _menuFactory;

  public BubbleViewFactory(IconCacheService iconCacheService, MenuFactory menuFactory) {
    _iconCacheService = iconCacheService ?? throw new ArgumentNullException(nameof(iconCacheService));
    _menuFactory = menuFactory ?? throw new ArgumentNullException(nameof(menuFactory));
  }

  public FrameworkElement CreateBubble(
    BubbleItem data,
    MouseButtonEventHandler onLeftButtonDown,
    Action<BubbleItem> onDeleteRequested) {
    Grid container = new Grid {
      Width = 75,
      Height = 75,
      Background = Brushes.Transparent,
      RenderTransformOrigin = new Point(0.5, 0.5),
      RenderTransform = new ScaleTransform(0, 0),
      SnapsToDevicePixels = true,
      UseLayoutRounding = true,
      Tag = data
    };

    Ellipse circle = new Ellipse {
      Fill = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20)),
      Stroke = Brushes.Cyan,
      StrokeThickness = 1.5,
      Effect = new DropShadowEffect {
        Color = Colors.Cyan,
        BlurRadius = 5,
        ShadowDepth = 0,
        Opacity = 0.4
      }
    };

    Image img = new Image {
      Source = _iconCacheService.GetIcon(data),
      Width = 32,
      Height = 32,
      VerticalAlignment = VerticalAlignment.Center,
      Margin = new Thickness(0, 0, 0, 15),
      SnapsToDevicePixels = true,
      UseLayoutRounding = true
    };
    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

    if (data.SubItems.Count > 0 || data.Path == BubbleConstants.CollectionPath) {
      img.Source = _iconCacheService.GetIcon(data);
      circle.Stroke = Brushes.Gold;
    }

    TextBlock txt = new TextBlock {
      Text = data.Name,
      Foreground = Brushes.White,
      FontSize = 9,
      VerticalAlignment = VerticalAlignment.Bottom,
      HorizontalAlignment = HorizontalAlignment.Center,
      Margin = new Thickness(5, 0, 5, 8),
      TextTrimming = TextTrimming.CharacterEllipsis,
      MaxWidth = 60
    };

    circle.IsHitTestVisible = false;
    img.IsHitTestVisible = false;
    txt.IsHitTestVisible = false;

    container.Children.Add(circle);
    container.Children.Add(img);
    container.Children.Add(txt);

    container.MouseRightButtonUp += (s, e) => {
      var menu = _menuFactory.CreateBubbleMenu(() => onDeleteRequested(data));
      menu.IsOpen = true;
      e.Handled = true;
    };

    container.MouseLeftButtonDown += onLeftButtonDown;
    return container;
  }
}
