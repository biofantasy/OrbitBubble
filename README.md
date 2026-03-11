# OrbitBubble

OrbitBubble 是一個以 WPF 實作的桌面捷徑泡泡選單，支援：
- 全域熱鍵開關選單（預設 `Alt + Space`）
- 滑鼠手勢開關選單
- 拖放檔案建立泡泡
- 泡泡拖曳合併成集合
- 集合層級瀏覽與返回

## 環境需求

- Windows 10/11
- .NET SDK 10
- 目標框架：`net10.0-windows10.0.19041.0`

## 執行方式

在專案根目錄執行：

```bash
dotnet build
dotnet run --project OrbitBubble.csproj
```

## 測試

測試專案位於 `tests/OrbitBubble.Tests`，可用以下指令執行：

```bash
dotnet test OrbitBubble.slnx
```

目前已涵蓋核心服務測試：
- `BubbleLayoutService`
- `BubbleStateService`
- `GestureService`
- `WindowRuntimeService`
- `BubbleInteractionService`

## 專案結構（重構後）

- `Core/Models`：領域模型與常數（如 `BubbleItem`、`BubbleConstants`）
- `Core/Repositories`：資料存取（`IBubbleRepository`、`BubbleRepository`）
- `Core/Managers`：系統層管理（熱鍵註冊等）
- `Core/Helpers`：低階輔助與擴充方法（滑鼠 Hook、UI extension）
- `Core/Services`：核心業務與 UI 協調邏輯
  - `GestureService`：手勢判定
  - `BubbleLayoutService`：泡泡佈局計算
  - `BubbleInteractionService`：拖曳/合併判定
  - `BubbleStateService`：資料狀態與導航堆疊
  - `IconCacheService`：圖示快取
  - `MenuAnimationService`：開關動畫
  - `MenuFactory` / `BubbleViewFactory`：UI 元件建立
  - `WindowRuntimeService`：視窗執行期座標與訊息判定
- `MainWindow.xaml(.cs)`：視圖與流程協調
- `App.xaml(.cs)`：啟動組裝（composition root）

## 已修正的重要問題

- 泡泡不跟隨中心移動：已改為使用 `CenterHub` 即時中心當作軌道圓心
- 關閉後再次開啟中心偏移：已修正對齊邏輯，並加入測試保護
- `NativeMethods.txt` 噪音警告：已清理為實際使用 API，建置維持 0 警告