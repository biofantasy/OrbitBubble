using OrbitBubble.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OrbitBubble.Core.Managers;

public class BubbleDataManager {

  private readonly static string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bubbles_config.json");

  public static void SaveData(List<BubbleItem> _allBubbles) {
    try {
      var options = new JsonSerializerOptions { WriteIndented = true };
      string json = JsonSerializer.Serialize(_allBubbles, options);
      File.WriteAllText(_configPath, json);
    } catch (Exception ex) {
      // 靜默失敗或記錄
      System.Windows.MessageBox.Show($"無法儲存路徑: {ex.Message}");
    }
  }

  public static (List<BubbleItem>, List<BubbleItem>) LoadData() {

    List<BubbleItem> _allBubbles = new(), _currentViewBubbles = new();

    try {
      if (File.Exists(_configPath)) {
        string json = File.ReadAllText(_configPath);
        var data = JsonSerializer.Deserialize<List<BubbleItem>>(json);
        if (data != null) {
          _allBubbles = data;
          _currentViewBubbles = new List<BubbleItem>(_allBubbles);
        }
      }
    } catch {
      _allBubbles = new List<BubbleItem>();
    }

    return (_allBubbles, _currentViewBubbles);
  }
}
