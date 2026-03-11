namespace OrbitBubble.Core.Managers;

public interface IHotkeyManager {
  int HotkeyId { get; }
  void Register(nint handle);
  void Unregister();
}
