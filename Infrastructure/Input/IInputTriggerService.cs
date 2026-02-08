namespace OrbitBubble.Infrastructure.Input;

public interface IInputTriggerService : IDisposable {

  event EventHandler<MenuRequestedEventArgs> MenuRequested;
  event EventHandler<PointerSampledEventArgs> PointerSampled;

  void Start(nint windowHandle);
  void Stop();

  /// <summary>
  /// 統一入口：任何來源想開/關選單，都呼叫這個
  /// </summary>
  void RequestMenu(MenuRequestSource source);
}

public sealed class PointerSampledEventArgs : EventArgs {
  public PointerSampledEventArgs(double xDip, double yDip) {
    XDip = xDip;
    YDip = yDip;
  }

  public double XDip { get; }
  public double YDip { get; }
}

public enum MenuRequestSource {
  Hotkey,
  Gesture,
  CenterClick,
  Unknown
}

public sealed class MenuRequestedEventArgs : EventArgs {
  public MenuRequestedEventArgs(MenuRequestSource source) => Source = source;
  public MenuRequestSource Source { get; }
}