using OrbitBubble.Core.Models;
using System.Windows;

namespace OrbitBubble.Views;

public partial class CollectionMergeModeDialog : Window {
  public CollectionMergeMode? SelectedMode { get; private set; }

  public CollectionMergeModeDialog() {
    InitializeComponent();
  }

  private void Flatten_Click(object sender, RoutedEventArgs e) {
    SelectedMode = CollectionMergeMode.FlattenItems;
    DialogResult = true;
  }

  private void Keep_Click(object sender, RoutedEventArgs e) {
    SelectedMode = CollectionMergeMode.KeepCollectionAsItem;
    DialogResult = true;
  }

  private void Cancel_Click(object sender, RoutedEventArgs e) {
    SelectedMode = null;
    DialogResult = false;
  }
}
