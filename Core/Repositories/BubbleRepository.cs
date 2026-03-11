using OrbitBubble.Core.Models;
using System.IO;
using System.Text.Json;

namespace OrbitBubble.Core.Repositories;

public class BubbleRepository : IBubbleRepository {
  private static readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bubbles_config.json");

  public List<BubbleItem> LoadAll() {
    try {
      if (!File.Exists(_configPath)) {
        return new List<BubbleItem>();
      }

      string json = File.ReadAllText(_configPath);
      return JsonSerializer.Deserialize<List<BubbleItem>>(json) ?? new List<BubbleItem>();
    } catch {
      return new List<BubbleItem>();
    }
  }

  public void SaveAll(List<BubbleItem> bubbles) {
    try {
      var options = new JsonSerializerOptions { WriteIndented = true };
      string json = JsonSerializer.Serialize(bubbles, options);
      File.WriteAllText(_configPath, json);
    } catch (Exception ex) {
      System.Windows.MessageBox.Show($"無法儲存路徑: {ex.Message}");
    }
  }
}
