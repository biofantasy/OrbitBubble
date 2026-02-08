using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrbitBubble.Core.Common;
using OrbitBubble.Domain.Bubbles;


namespace OrbitBubble.Core.Stores;

public sealed class JsonBubbleStore : IBubbleStore {
  private readonly ILogger<JsonBubbleStore> _logger;
  private readonly string _filePath;

  private static readonly JsonSerializerOptions _jsonOptions = new() {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
  };

  public JsonBubbleStore(ILogger<JsonBubbleStore> logger) {
    _logger = logger;

    var dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrbitBubble");

    Directory.CreateDirectory(dir);
    _filePath = Path.Combine(dir, "bubbles.json");
  }

  public Result<BubbleRoot> Load() {
    try {
      if (!File.Exists(_filePath)) {
        _logger.LogInformation("Bubble store not found; returning empty. path={Path}", _filePath);
        return Result<BubbleRoot>.Ok(new BubbleRoot { Version = 1 });
      }

      var json = File.ReadAllText(_filePath);
      var root = JsonSerializer.Deserialize<BubbleRoot>(json, _jsonOptions);

      if (root == null)
        return Result<BubbleRoot>.Fail(new StoreError("STORE_DESERIALIZE_NULL", "Deserialize returned null."));

      // 版本檢查（先留 stub）
      if (root.Version <= 0)
        root.Version = 1;

      _logger.LogInformation("Bubble store loaded. path={Path} version={Version} items={Count}",
          _filePath, root.Version, root.Items?.Count ?? 0);

      return Result<BubbleRoot>.Ok(root);
    } catch (JsonException ex) {
      _logger.LogError(ex, "Bubble store JSON parse failed. path={Path}", _filePath);
      return Result<BubbleRoot>.Fail(new StoreError("STORE_JSON_PARSE", "JSON parse failed.", ex));
    } catch (IOException ex) {
      _logger.LogError(ex, "Bubble store IO failed. path={Path}", _filePath);
      return Result<BubbleRoot>.Fail(new StoreError("STORE_IO", "IO error while loading store.", ex));
    } catch (Exception ex) {
      _logger.LogError(ex, "Bubble store load failed. path={Path}", _filePath);
      return Result<BubbleRoot>.Fail(new StoreError("STORE_UNKNOWN", "Unknown error while loading store.", ex));
    }
  }

  public Result<bool> Save(BubbleRoot root) {
    try {
      root.Version = Math.Max(root.Version, 1);

      var json = JsonSerializer.Serialize(root, _jsonOptions);

      // 保守：寫到 temp 再 replace，避免寫一半壞檔
      var tmp = _filePath + ".tmp";
      File.WriteAllText(tmp, json);
      File.Copy(tmp, _filePath, overwrite: true);
      File.Delete(tmp);

      _logger.LogInformation("Bubble store saved. path={Path} version={Version} items={Count}",
          _filePath, root.Version, root.Items?.Count ?? 0);

      return Result<bool>.Ok(true);
    } catch (IOException ex) {
      _logger.LogError(ex, "Bubble store IO failed on save. path={Path}", _filePath);
      return Result<bool>.Fail(new StoreError("STORE_IO", "IO error while saving store.", ex));
    } catch (Exception ex) {
      _logger.LogError(ex, "Bubble store save failed. path={Path}", _filePath);
      return Result<bool>.Fail(new StoreError("STORE_UNKNOWN", "Unknown error while saving store.", ex));
    }
  }
}
