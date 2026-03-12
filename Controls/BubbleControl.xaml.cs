using OrbitBubble.Core.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OrbitBubble.Controls;

public partial class BubbleControl : UserControl {
  public static readonly DependencyProperty IconSourceProperty =
    DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(BubbleControl));

  public static readonly DependencyProperty LabelProperty =
    DependencyProperty.Register(nameof(Label), typeof(string), typeof(BubbleControl), new PropertyMetadata(string.Empty));

  public static readonly DependencyProperty AccentColorProperty =
    DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(BubbleControl), new PropertyMetadata(Colors.Cyan, OnAccentColorChanged));

  public static readonly DependencyProperty AccentBrushProperty =
    DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(BubbleControl), new PropertyMetadata(Brushes.Cyan));

  public static readonly DependencyProperty LabelBrushProperty =
    DependencyProperty.Register(nameof(LabelBrush), typeof(Brush), typeof(BubbleControl), new PropertyMetadata(Brushes.White));

  public static readonly DependencyProperty BodyMiddleColorProperty =
    DependencyProperty.Register(nameof(BodyMiddleColor), typeof(Color), typeof(BubbleControl), new PropertyMetadata(Color.FromArgb(150, 86, 170, 255)));

  public static readonly DependencyProperty BodyEdgeColorProperty =
    DependencyProperty.Register(nameof(BodyEdgeColor), typeof(Color), typeof(BubbleControl), new PropertyMetadata(Color.FromArgb(110, 18, 58, 96)));

  public static readonly DependencyProperty QualityModeProperty =
    DependencyProperty.Register(nameof(QualityMode), typeof(UiQualityMode), typeof(BubbleControl), new PropertyMetadata(UiQualityMode.Balanced, OnQualityModeChanged));

  public static readonly DependencyProperty GlowBlurRadiusProperty =
    DependencyProperty.Register(nameof(GlowBlurRadius), typeof(double), typeof(BubbleControl), new PropertyMetadata(14d));

  public static readonly DependencyProperty GlowOpacityProperty =
    DependencyProperty.Register(nameof(GlowOpacity), typeof(double), typeof(BubbleControl), new PropertyMetadata(0.72d));

  public static readonly DependencyProperty BodyOpacityProperty =
    DependencyProperty.Register(nameof(BodyOpacity), typeof(double), typeof(BubbleControl), new PropertyMetadata(0.8d));

  public static readonly DependencyProperty HighlightBlurRadiusProperty =
    DependencyProperty.Register(nameof(HighlightBlurRadius), typeof(double), typeof(BubbleControl), new PropertyMetadata(6d));

  public static readonly DependencyProperty HighlightOpacityProperty =
    DependencyProperty.Register(nameof(HighlightOpacity), typeof(double), typeof(BubbleControl), new PropertyMetadata(0.8d));

  public static readonly DependencyProperty IsAvailableProperty =
    DependencyProperty.Register(nameof(IsAvailable), typeof(bool), typeof(BubbleControl), new PropertyMetadata(true, OnAvailabilityChanged));

  public static readonly DependencyProperty UnavailableBadgeVisibilityProperty =
    DependencyProperty.Register(nameof(UnavailableBadgeVisibility), typeof(Visibility), typeof(BubbleControl), new PropertyMetadata(Visibility.Collapsed));

  public static readonly DependencyProperty BubbleSizeProperty =
    DependencyProperty.Register(nameof(BubbleSize), typeof(double), typeof(BubbleControl), new PropertyMetadata(88d));

  public event MouseButtonEventHandler? BubbleMouseLeftButtonDown;
  public event MouseButtonEventHandler? BubbleMouseRightButtonUp;

  public BubbleControl() {
    InitializeComponent();
  }

  public ImageSource? IconSource {
    get => (ImageSource?)GetValue(IconSourceProperty);
    set => SetValue(IconSourceProperty, value);
  }

  public string Label {
    get => (string)GetValue(LabelProperty);
    set => SetValue(LabelProperty, value);
  }

  public Color AccentColor {
    get => (Color)GetValue(AccentColorProperty);
    set => SetValue(AccentColorProperty, value);
  }

  public Brush AccentBrush {
    get => (Brush)GetValue(AccentBrushProperty);
    private set => SetValue(AccentBrushProperty, value);
  }

  public Brush LabelBrush {
    get => (Brush)GetValue(LabelBrushProperty);
    private set => SetValue(LabelBrushProperty, value);
  }

  public Color BodyMiddleColor {
    get => (Color)GetValue(BodyMiddleColorProperty);
    private set => SetValue(BodyMiddleColorProperty, value);
  }

  public Color BodyEdgeColor {
    get => (Color)GetValue(BodyEdgeColorProperty);
    private set => SetValue(BodyEdgeColorProperty, value);
  }

  public double BubbleSize {
    get => (double)GetValue(BubbleSizeProperty);
    set => SetValue(BubbleSizeProperty, value);
  }

  public UiQualityMode QualityMode {
    get => (UiQualityMode)GetValue(QualityModeProperty);
    set => SetValue(QualityModeProperty, value);
  }

  public double GlowBlurRadius {
    get => (double)GetValue(GlowBlurRadiusProperty);
    private set => SetValue(GlowBlurRadiusProperty, value);
  }

  public double GlowOpacity {
    get => (double)GetValue(GlowOpacityProperty);
    private set => SetValue(GlowOpacityProperty, value);
  }

  public double BodyOpacity {
    get => (double)GetValue(BodyOpacityProperty);
    private set => SetValue(BodyOpacityProperty, value);
  }

  public double HighlightBlurRadius {
    get => (double)GetValue(HighlightBlurRadiusProperty);
    private set => SetValue(HighlightBlurRadiusProperty, value);
  }

  public double HighlightOpacity {
    get => (double)GetValue(HighlightOpacityProperty);
    private set => SetValue(HighlightOpacityProperty, value);
  }

  public bool IsAvailable {
    get => (bool)GetValue(IsAvailableProperty);
    set => SetValue(IsAvailableProperty, value);
  }

  public Visibility UnavailableBadgeVisibility {
    get => (Visibility)GetValue(UnavailableBadgeVisibilityProperty);
    private set => SetValue(UnavailableBadgeVisibilityProperty, value);
  }

  private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is BubbleControl control && e.NewValue is Color c) {
      if (control.IsAvailable) {
        control.ApplyAccentPalette(c);
      }
    }
  }

  private static void OnQualityModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is BubbleControl control) {
      control.ApplyQualityPreset();
    }
  }

  private void ApplyQualityPreset() {
    switch (QualityMode) {
      case UiQualityMode.Pretty:
        GlowBlurRadius = 22;
        GlowOpacity = 0.8;
        BodyOpacity = 0.84;
        HighlightBlurRadius = 7;
        HighlightOpacity = 0.88;
        break;
      case UiQualityMode.Performance:
        GlowBlurRadius = 10;
        GlowOpacity = 0.38;
        BodyOpacity = 0.68;
        HighlightBlurRadius = 3;
        HighlightOpacity = 0.48;
        break;
      default:
        GlowBlurRadius = 17;
        GlowOpacity = 0.62;
        BodyOpacity = 0.74;
        HighlightBlurRadius = 5;
        HighlightOpacity = 0.68;
        break;
    }

    ApplyAvailabilityVisual();
  }

  private void ApplyAccentPalette(Color c) {
    AccentBrush = new SolidColorBrush(c);
    BodyMiddleColor = Color.FromArgb(170, c.R, c.G, c.B);
    BodyEdgeColor = Color.FromArgb(115,
      (byte)(c.R * 0.35),
      (byte)(c.G * 0.35),
      (byte)(c.B * 0.35));
  }

  private static void OnAvailabilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
    if (d is BubbleControl control && e.NewValue is bool available) {
      control.UnavailableBadgeVisibility = available ? Visibility.Collapsed : Visibility.Visible;
      control.ToolTip = available ? null : "連結已失效（檔案或資料夾不存在）";
      control.ApplyQualityPreset();
    }
  }

  private void ApplyAvailabilityVisual() {
    if (IsAvailable) {
      ApplyAccentPalette(AccentColor);
      LabelBrush = Brushes.White;
      Opacity = 1.0;
      return;
    }

    AccentBrush = new SolidColorBrush(Color.FromArgb(130, 175, 175, 175));
    BodyMiddleColor = Color.FromArgb(165, 185, 185, 185);
    BodyEdgeColor = Color.FromArgb(120, 82, 82, 82);
    LabelBrush = new SolidColorBrush(Color.FromArgb(230, 232, 232, 232));

    GlowOpacity = Math.Min(GlowOpacity, 0.18);
    BodyOpacity = Math.Min(BodyOpacity, 0.72);
    HighlightOpacity = Math.Min(HighlightOpacity, 0.35);
    HighlightBlurRadius = Math.Max(HighlightBlurRadius, 4);
    Opacity = 0.95;
  }

  private void BubbleButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
    BubbleMouseLeftButtonDown?.Invoke(this, e);
  }

  private void BubbleButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
    BubbleMouseRightButtonUp?.Invoke(this, e);
  }
}
