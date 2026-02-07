# OrbitBubble (環繞泡泡選單)

## 1. 專案概述

本專案旨在開發一個 Windows 11/10 系統下的現代化導航工具。透過自定義的滑鼠手勢（旋轉）或全域熱鍵觸發一個放射狀的泡泡選單，提供直覺的檔案瀏覽與系統操作體驗。

## 2. 核心架構與技術棧

+ 目標平台： Windows 10 / 11 (x64)
+ 開發框架： .NET 8 / WPF (Windows Presentation Foundation)
+ 渲染技術： Composition API (用於毛玻璃與流體動畫)
+ 系統交互： Win32 API (User32.dll) Hook 機制

## 3. 功能需求規範

### 3.1 觸發機制 (Triggering)

+ 滑鼠手勢：
    - 監聽滑鼠 WH_MOUSE_LL。
    - 演算法： 記錄連續 300ms 內的座標點，計算是否形成一個閉合圓形（容錯率 20%）。
    - 觸發後，於滑鼠當前座標中心生成選單。
+ 全域熱鍵：
    - 支援用戶自定義組合鍵（預設：Alt + Space）。
    - 使用 RegisterHotKey 實現背景監聽。

### 3.2 介面邏輯 (UI/UX)

+ 中心圓 (Center Hub)：
    - 顯示當前路徑名稱或圖標。
    - 左鍵點擊： 回到上一層資料夾。
    - 右鍵點擊： 彈出「系統設定」選單。

+ 浮游泡泡 (Orbiting Bubbles)：
    - 圍繞中心圓等距分佈。
    - 泡泡內顯示檔案/資料夾名稱與縮圖。
    - 動畫效果： 進入時由中心向外彈出 (Ease-out back)，閒置時有微小的上下浮動感 (Sine wave)。
    - 層級導航： 點擊資料夾泡泡，舊泡泡向外淡出，新層級泡泡由中心彈出。

### 3.3 設定選單 (Configuration)

透過中心圓右鍵觸發，功能包括：
+ 手勢設定： 開啟/關閉旋轉觸發、調整辨識靈敏度。
+ 熱鍵管理： 錄製新的組合鍵。
+ 視覺自訂： 泡泡大小、顏色主題（深色/淺色/跟隨系統）、透明度調整。
+ 黑名單： 指定程式在執行時自動禁用手勢（如遊戲時避免誤觸）。

## 4. 資料與效能設計

+ 非同步載入： 檔案圖標與路徑讀取必須在背景執行緒完成，不得阻塞 UI 動畫。
+ 快取機制： 已讀取的檔案圖標需快取於記憶體中，提升重複開啟的速度。
+ 低耗能模式： 選單關閉時，滑鼠 Hook 僅進行輕量計算；選單顯示時才啟動 60 FPS 渲染。

## 5. 風險評估

1. 防毒軟體誤判： 全域滑鼠 Hook 可能被部分防毒體視為惡意行為。解決方案：申請數位簽章或提供白名單指引。

2. 效能瓶頸： 當資料夾內有數百個檔案時，圓形選單會過於擁擠。解決方案：限制單層顯示上限（如前 12 個），並提供「更多...」泡泡。


====2026/02/07 更新===

# OrbitBubble 專案開發規格書 (OrbitBubble Project Specification)

## 1. 專案概述
OrbitBubble 是一個基於 WPF 的全螢幕桌面工具，提供一個圓形軌道選單。使用者透過全域快捷鍵或滑鼠手勢喚出選單，並能以拖拽、合併的方式管理檔案或資料夾圖示。

## 2. UI 階層架構 (UI Hierarchy)
專案採用「扁平化舞台」架構，以確保座標計算的一致性與動畫效能。層級元件名稱功能說明
Level 1MainWindow透明、置頂、覆蓋全螢幕的容器 (VirtualScreen)。
Level 2MainCanvas捕捉全域滑鼠點擊與負責整體透明度動畫的畫布。
Level 3AnimationWrapper核心舞台 (800x800)。負責縮放與旋轉動畫，並跟隨滑鼠位置移動。
Level 4CenterHub固定於舞台中心 (350, 350) 的導航圓餅，顯示目前目錄名稱。
Level 4Bubbles動態生成的項目，相對於舞台中心 (400, 400) 計算圓周位置。

## 3. 核心功能規格

### A. 舞台定位邏輯 (UpdatePositionToMouse)
當觸發顯示時，舞台 AnimationWrapper 會移動至滑鼠位置：
+ 計算公式：Left = Mouse.X - 400Top = Mouse.Y - 400
+ 目的：確保滑鼠精確指向 CenterHub 的正中心。

### B. 彈出動畫規格 (ShowMenuWithAnimation)
採用非同步渲染確保流暢度。
+ 縮放動畫 (ScaleTransform)：$0.0 \rightarrow 1.0$，使用 BackEase (Amplitude: 0.5) 達成彈跳效果。
+ 旋轉動畫 (RotateTransform)：$-180^\circ \rightarrow 0^\circ$，使用 QuarticEaseOut。
+ 透明度動畫：$0 \rightarrow 1$，時間約 0.4 秒。

### C. 佈局演算法 (AddBubble)
泡泡在 800x800 的舞台內以圓周排列：
+ 圓心：$(400, 400)$
+ 半徑：$180 \text{ pixels}$
+ 角度計算：$\theta = \frac{Index \times 2\pi}{TotalCount}$
+ 泡泡座標：
    - $X = 400 + 180 \times \cos(\theta) - \text{HalfSize}$$
    - Y = 400 + 180 \times \sin(\theta) - \text{HalfSize}$
    
### D. 互動手勢機制 (DetectCircleGesture)
+ 順時針旋轉 (累積角度 $> 130^\circ$)：開啟選單。
+ 逆時針旋轉 (累積角度 $< -130^\circ$)：隱藏選單。
+ 手勢重置：當滑鼠停止移動或軌跡點超過 50 點時清空累計值。

## 4. 資料模型 (Data Model)
使用 BubbleItem 類別管理資料，支援巢狀結構。

```C#
public class BubbleItem {
    public string Name { get; set; }        // 顯示名稱
    public string Path { get; set; }        // 檔案路徑或類型(Collection)
    public List<BubbleItem> SubItems { get; set; } // 子項目（用於資料夾合併）
}
```

## 5. 操作邏輯與快捷鍵
+ 呼叫/關閉：Alt + Space 或 滑鼠旋轉手勢。
+ 進入資料夾：雙擊泡泡項目。
+ 返回上層：點擊 CenterHub。
+ 新增項目：將外部檔案拖入 CenterHub。
+ 合併項目：將一個泡泡拖至另一個泡泡上方（距離 < 50px）。
+ 刪除項目：右鍵點擊泡泡並選擇刪除。

## 6. 技術環境
+ 框架：.NET 10 / WPF
+ 外部調用：user32.dll (用於全域熱鍵與滑鼠 Hook)
+ 渲染優化：CompositionTarget.Rendering 影格同步、RenderTransformOrigin="0.5,0.5"。