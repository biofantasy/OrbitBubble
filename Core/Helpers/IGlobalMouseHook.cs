namespace OrbitBubble.Core.Helpers;

public interface IGlobalMouseHook {
  event Action<int, int>? MouseMoved;
  void Install();
  void Uninstall();
}
