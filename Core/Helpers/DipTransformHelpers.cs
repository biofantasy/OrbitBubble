using System;
using System.Windows;
using System.Windows.Media;

namespace OrbitBubble.Core.Helpers;
  public static class DipTransformHelpers {
    private static Matrix? _fromDevice;

    public static void InitializeFromVisual(Visual visual) {
      var source = PresentationSource.FromVisual(visual);
      if (source?.CompositionTarget == null) return;

      _fromDevice = source.CompositionTarget.TransformFromDevice;
    }

    public static (double xDip, double yDip) TransformDeviceToDip(int xDevice, int yDevice) {
      if (_fromDevice == null) {
        // fallback（不準但不中斷）
        return (xDevice, yDevice);
      }

      var p = _fromDevice.Value.Transform(new Point(xDevice, yDevice));
      return (p.X, p.Y);
    }
  }
