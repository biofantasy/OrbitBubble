新一版規格書（OrbitBubble v2 Spec）
1. 產品目標

在 Windows 10/11 提供「快速啟動圓形選單」

以熱鍵/手勢/中心點擊開啟選單

支援層級導航、拖拽合併成 Collection、拖放新增

2. 非功能性要求（KPI）

CPU（待機/關閉狀態）：≤ 1%（平均），不因滑鼠移動明顯上升

開啟選單延遲：≤ 50ms（不含首次 icon 冷啟）

Icon 冷啟：單顆 icon 不阻塞 UI；可見泡泡 icon 在 1s 內逐步補齊

UI 無凍結：主執行緒不得因 IO/Win32 icon 讀取而 freeze > 50ms

穩定性：全域例外均會落 log；非致命錯誤不應直接閃退

3. 架構（MVVM + Service 分層）
3.1 Layers

Domain

BubbleItem, BubbleItemType, GestureResult, GestureOptions

Application

Interfaces：IInputTriggerService, IGestureDetector, IBubbleStore, IIconProvider, IBubbleLayoutEngine

Use cases：Open/Close/Back/OpenItem/AddItems/Merge

Infrastructure

Win32 Hook/Hotkey、Shell Icon、JSON Store（含 version）

Wpf

Views + ViewModels + Behaviors

3.2 核心元件責任

MainViewModel

狀態：IsMenuOpen, VisibleBubbles, NavHistory, CurrentContext

指令：Toggle/Open/Back/AddItems/Merge/OpenSettings/Exit

BubbleViewModel

可綁定資料：Id, DisplayName, Type, Path, Icon, X, Y, IsHighlighted

IInputTriggerService

提供事件：MenuRequested, PointerSampled(Point dip)

必須節流，Hook callback 必須極速返回

必須處理 DPI 轉換

IGestureDetector

Detect(samples, options) -> GestureResult

支援參數調整與統計

IIconProvider

GetIconAsync(path/type) -> ImageSource

記憶體快取 + 併發限制 + placeholder

IBubbleStore

Load/Save，JSON schema version，錯誤碼

4. 觸發與手勢規則

觸發來源：

Hotkey（預設：可設定）

手勢（預設：畫圈）

中心 Hub 點擊

手勢採樣：

Latest-wins 節流（16–20ms）+ 距離門檻（2–4 DIP）

保留最近 1.0–1.5 秒點位，ring buffer 上限 64–96

偵測：

背景可算，但不得每點觸發一次偵測（以批次為主）

5. 泡泡資料模型

BubbleItem

Id: string

DisplayName: string

Type: BubbleItemType

Path: string?（File/Folder 可用；Collection/Command 可為 null）

Children: List<BubbleItem>（Collection）

Metadata（可選：iconKey、createdAt、lastUsed…）

禁止使用 magic string 判斷類型（例如 "Collection"）

6. 佈局與呈現

舞台：800x800（可配置）

泡泡上限：12（超出顯示 “More…” 或多圈策略，v2 先定 “More…”）

佈局由 IBubbleLayoutEngine 計算座標（VM 只存 X/Y）

UI 以 ItemsControl + Canvas 呈現，避免手工 add/remove UI

7. 拖拽/Drop/合併

Drop 到中心：新增 bubble（去重）

拖拽 bubble 到中心：

若中心是「合併目標」則合併成 Collection（或加入既有 Collection）

播放合併動畫（UI Behavior）

規則與資料變更在 VM；命中測試/動畫在 Behavior

8. 設定與持久化

設定檔：%AppData%/OrbitBubble/settings.json

資料檔：%AppData%/OrbitBubble/bubbles.json（含 version）

支援未來 schema migration（v2 規格需保留 version）

9. 日誌與錯誤處理（Serilog）

日誌框架：Serilog（File rolling）

Log 內容要求：

Input：hook install/uninstall、hotkey register、採樣率統計（debug）

Gesture：成功/失敗、耗時、點數、參數

Icon：cache hit/miss、耗時、例外 path

Store：load/save 成功、版本、bubble count、錯誤碼

全域例外：WPF/Task/AppDomain 必須捕捉並落 log

Service 禁止 MessageBox；UI 決定提示方式