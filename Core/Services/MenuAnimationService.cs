using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OrbitBubble.Core.Services;

public class MenuAnimationService {
  private const double ClosedRotationAngle = -35;
  private const double OpenRotationAngle = 0;

  public void PlayHide(Canvas mainCanvas, ScaleTransform mainScale, RotateTransform mainRotate, Action onCompleted) {
    Duration duration = new Duration(TimeSpan.FromSeconds(0.3));
    IEasingFunction ease = new QuarticEase { EasingMode = EasingMode.EaseIn };

    // 先解除前一段動畫鎖定，避免沿用異常角度造成旋轉突增
    mainScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    mainScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    mainRotate.BeginAnimation(RotateTransform.AngleProperty, null);

    var currentScaleX = Math.Clamp(mainScale.ScaleX, 0, 1);
    var currentScaleY = Math.Clamp(mainScale.ScaleY, 0, 1);
    var currentAngle = NormalizeAngle(mainRotate.Angle);

    mainScale.ScaleX = currentScaleX;
    mainScale.ScaleY = currentScaleY;
    mainRotate.Angle = currentAngle;

    DoubleAnimation scaleAnimX = new DoubleAnimation(currentScaleX, 0, duration) { EasingFunction = ease };
    DoubleAnimation scaleAnimY = new DoubleAnimation(currentScaleY, 0, duration) { EasingFunction = ease };
    DoubleAnimation rotateAnim = new DoubleAnimation {
      From = currentAngle,
      To = ClosedRotationAngle,
      Duration = duration,
      EasingFunction = ease
    };

    scaleAnimX.Completed += (_, _) => onCompleted();

    mainScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimX);
    mainScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimY);
    mainRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
  }

  public void PrepareShowState(Window window, Canvas mainCanvas, ScaleTransform mainScale, RotateTransform mainRotate) {
    window.Visibility = Visibility.Visible;
    mainCanvas.Opacity = 0;

    mainScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
    mainScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
    mainRotate.BeginAnimation(RotateTransform.AngleProperty, null);

    mainScale.ScaleX = 0;
    mainScale.ScaleY = 0;
    mainRotate.Angle = ClosedRotationAngle;
  }

  public void PlayShow(Canvas mainCanvas, ScaleTransform mainScale, RotateTransform mainRotate) {
    Duration duration = new Duration(TimeSpan.FromSeconds(0.4));
    IEasingFunction backEase = new BackEase { Amplitude = 0.32, EasingMode = EasingMode.EaseOut };
    IEasingFunction quartEase = new QuarticEase { EasingMode = EasingMode.EaseOut };

    var expandAnim = new DoubleAnimation(0, 1, duration) { EasingFunction = backEase };
    var opacityAnim = new DoubleAnimation(0, 1, duration);
    var rotateAnim = new DoubleAnimation(ClosedRotationAngle, OpenRotationAngle, duration) { EasingFunction = quartEase };
    Timeline.SetDesiredFrameRate(expandAnim, 45);

    mainCanvas.BeginAnimation(Canvas.OpacityProperty, opacityAnim);
    mainScale.BeginAnimation(ScaleTransform.ScaleXProperty, expandAnim);
    mainScale.BeginAnimation(ScaleTransform.ScaleYProperty, expandAnim);
    mainRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
  }

  private static double NormalizeAngle(double angle) {
    var normalized = angle % 360;
    if (normalized > 180) normalized -= 360;
    if (normalized < -180) normalized += 360;
    return normalized;
  }
}
