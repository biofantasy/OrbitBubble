using System.Windows.Media;

namespace OrbitBubble.Core.Icons;

public interface IIconProvider {
  ImageSource Placeholder { get; }

  /// <summary>
  /// 非同步取得 icon。不得阻塞 UI thread。
  /// </summary>
  Task<ImageSource> GetIconAsync(string path, CancellationToken ct = default);
}
