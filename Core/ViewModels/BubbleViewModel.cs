using CommunityToolkit.Mvvm.ComponentModel;
using OrbitBubble.Domain.Bubbles;
using System.Windows.Media;

namespace OrbitBubble.Core.ViewModels;

public partial class BubbleViewModel : ObservableObject {
  public BubbleViewModel(string id, string displayName, BubbleItemType type, string? path) {
    Id = id;
    DisplayName = displayName;
    Type = type;
    Path = path;
  }

  public string Id { get; }
  public string DisplayName { get; }
  public BubbleItemType Type { get; }
  public string? Path { get; }

  [ObservableProperty] private double x;
  [ObservableProperty] private double y;

  [ObservableProperty] private ImageSource? icon;
}
