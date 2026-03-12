using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OrbitBubble.Core.Services;

public class MenuFactory {
  public ContextMenu CreateCenterHubMenu(Action onExitRequested) {
    var menu = new ContextMenu();
    var setHotkey = new MenuItem { Header = "設定熱鍵 (Alt+Space)" };
    var setGesture = new MenuItem { Header = "手勢靈敏度設定" };
    var exitApp = new MenuItem { Header = "結束程式" };
    exitApp.Click += (_, _) => onExitRequested();

    menu.Items.Add(setHotkey);
    menu.Items.Add(setGesture);
    menu.Items.Add(new Separator());
    menu.Items.Add(exitApp);
    return menu;
  }

  public ContextMenu CreateBubbleMenu(Action onDeleteRequested, Action? onRenameRequested = null, Action? onRemoveInvalidRequested = null) {
    var menu = new ContextMenu();
    if (onRenameRequested != null) {
      var renameItem = new MenuItem { Header = "更改名稱" };
      renameItem.Click += (_, _) => onRenameRequested();
      menu.Items.Add(renameItem);
      menu.Items.Add(new Separator());
    }

    if (onRemoveInvalidRequested != null) {
      var removeInvalidItem = new MenuItem { Header = "移除失效連結", Foreground = Brushes.OrangeRed };
      removeInvalidItem.Click += (_, _) => onRemoveInvalidRequested();
      menu.Items.Add(removeInvalidItem);
      menu.Items.Add(new Separator());
    }

    var deleteItem = new MenuItem { Header = "刪除此泡泡", Foreground = Brushes.Red };
    deleteItem.Click += (_, _) => onDeleteRequested();
    menu.Items.Add(deleteItem);
    return menu;
  }
}
