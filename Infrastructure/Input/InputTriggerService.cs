using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Helpers;
using OrbitBubble.Core.Managers;

namespace OrbitBubble.Infrastructure.Input;

public sealed class InputTriggerService : IInputTriggerService {
  private readonly ILogger<InputTriggerService> _logger;
  private readonly Dispatcher _ui;
  private readonly GlobalMouseHook _mouseHook;
  private readonly HotkeyManager _hotkey;

  private volatile int _lastX;
  private volatile int _lastY;
  private volatile int _hasPoint; // 0/1

  private readonly DispatcherTimer _timer;

  private Matrix? _fromDevice; // device->DIP
  private readonly Stopwatch _rateSw = Stopwatch.StartNew();
  private int _samples;

  public event EventHandler<MenuRequestedEventArgs>? MenuRequested;
  public event EventHandler<PointerSampledEventArgs>? PointerSampled;

  public InputTriggerService(
      ILogger<InputTriggerService> logger,
      Dispatcher uiDispatcher,
      GlobalMouseHook mouseHook,
      HotkeyManager hotkeyManager) {
    _logger = logger;
    _ui = uiDispatcher;
    _mouseHook = mouseHook;
    _hotkey = hotkeyManager;

    _mouseHook.MouseMoved += OnMouseMoved;

    _timer = new DispatcherTimer(
        TimeSpan.FromMilliseconds(16),
        DispatcherPriority.Background,
        OnTick,
        _ui);
  }

  public void Start(nint windowHandle) {
    _logger.LogInformation("InputTriggerService starting. hwnd={Hwnd}", windowHandle);

    _hotkey.Register(windowHandle);
    _mouseHook.Install();

    _ui.Invoke(() =>
    {
      var win = System.Windows.Application.Current?.MainWindow;
      if (win != null) {
        var src = PresentationSource.FromVisual(win);
        if (src?.CompositionTarget != null) {
          _fromDevice = src.CompositionTarget.TransformFromDevice;
          _logger.LogInformation("DPI transform initialized.");
        } else {
          _logger.LogWarning("Cannot get CompositionTarget; DIP transform fallback enabled.");
        }
      }
    });

    _timer.Start();
    _logger.LogInformation("InputTriggerService started.");
  }

  public void Stop() {
    _logger.LogInformation("InputTriggerService stopping...");
    _timer.Stop();

    _hotkey.Unregister();
    _mouseHook.Uninstall();

    _logger.LogInformation("InputTriggerService stopped.");
  }

  public void RequestMenu(MenuRequestSource source) {
    _logger.LogInformation("Menu requested. source={Source}", source);
    MenuRequested?.Invoke(this, new MenuRequestedEventArgs(source));
  }

  public void Dispose() {
    Stop();
    _mouseHook.MouseMoved -= OnMouseMoved;
  }

  private void OnMouseMoved(int xDevice, int yDevice) {
    _lastX = xDevice;
    _lastY = yDevice;
    _hasPoint = 1;
  }

  private void OnTick(object? sender, EventArgs e) {
    if (_hasPoint == 0) return;

    var xDev = _lastX;
    var yDev = _lastY;
    _hasPoint = 0;

    var (xDip, yDip) = DeviceToDip(xDev, yDev);
    PointerSampled?.Invoke(this, new PointerSampledEventArgs(xDip, yDip));
    _samples++;

    if (_rateSw.ElapsedMilliseconds >= 1000) {
      _logger.LogDebug("Pointer sampled rate={Rate}/s", _samples);
      _samples = 0;
      _rateSw.Restart();
    }
  }

  private (double xDip, double yDip) DeviceToDip(int xDevice, int yDevice) {
    if (_fromDevice == null) return (xDevice, yDevice);

    var p = _fromDevice.Value.Transform(new Point(xDevice, yDevice));
    return (p.X, p.Y);
  }
}
