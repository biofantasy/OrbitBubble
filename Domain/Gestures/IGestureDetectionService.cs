namespace OrbitBubble.Domain.Gestures;

public interface IGestureDetectionService : IDisposable {
  void Start();
  void Stop();
}
