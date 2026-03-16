# 输入触发与热键系统

## 模块职责
- 管理全局热键、软件内热键、低级键盘钩子、Ctrl+CC、鼠标划词与剪贴板监听。
- 将触发事件统一路由到 `MainWindowViewModel` 命令。
- 根据热键可用状态与全屏策略同步托盘图标状态。

## 关键入口
- `STranslate/Core/HotkeySettings.cs`
  - `LazyInitialize()`：启动时应用 Ctrl+CC、增量翻译键、全局热键注册。
  - `HandleGlobalLogic()`：热键到命令的映射中心。
- `STranslate/Helpers/HotkeyMapper.cs`
  - `SetHotkey()`：NHotkey/ChefKeys 注册。
  - `StartGlobalKeyboardMonitoring()`：低级键盘钩子（WH_KEYBOARD_LL）。
  - `RegisterHoldKey()`：按住键增量翻译。
- `STranslate/Helpers/CtrlSameCHelper.cs`
  - 监听 Ctrl+C 双击（500ms 窗口）。
- `STranslate/Helpers/MouseKeyHelper.cs`
  - 鼠标拖拽结束后读取选中文本并触发事件。
- `STranslate/Helpers/ClipboardMonitor.cs`
  - `AddClipboardFormatListener` 监听剪贴板变更。
- `STranslate/Views/MainWindow.xaml`
  - `Window.InputBindings`：软件内热键（设置、历史、置顶、自动翻译等）。

## 核心流程
### 从入口到结果：全局热键触发命令
1. `HotkeySettings.RegisterHotkeys()` 对每个全局热键调用 `HandleGlobalLogic(propertyName)`。
2. `HandleGlobalLogic()` 通过 `HotkeyMapper.SetHotkey()` 注册系统热键并绑定命令回调。
3. 回调执行前经 `WithFullscreenCheck()`：
   - `DisableGlobalHotkeys == true` 时禁用。
   - `IgnoreHotkeysOnFullscreen == true` 且前台全屏时跳过。
4. 命令进入 `MainWindowViewModel`（例如截图翻译、图片翻译、静默 OCR、替换翻译、剪贴板监听切换）。

### 从入口到结果：增量翻译（按住键）
1. `IncrementalTranslateKey` 变化触发 `ApplyIncrementalTranslate()`。
2. 注册 `HotkeyMapper.RegisterHoldKey(key, OnIncKeyPressed, OnIncKeyReleased)` 并开启低级键盘钩子。
3. 按下时 `OnIncKeyPressed()`：置顶窗口 + 开启鼠标划词监听 + 缓存旧文本。
4. 松开时 `OnIncKeyReleased()`：关闭划词监听，若文本有变化则执行翻译。

### 从入口到结果：Ctrl+CC、鼠标划词、剪贴板监听
- Ctrl+CC：`CtrlSameCHelper` 监听全局按键，500ms 内双击 `Ctrl+C` 触发 `CrosswordTranslateByCtrlSameCHandler()`。
- 鼠标划词：`MouseKeyHelper` 在拖拽完成后读选中文本，触发 `ExecuteTranslate()`。
- 剪贴板监听：`ClipboardMonitor` 收到 `WM_CLIPBOARDUPDATE` 后读取文本，触发 `OnClipboardTextChanged -> ExecuteTranslate()`。

### 软件内热键
- 主窗口、OCR 窗口、图片翻译窗口通过 `InputBindings` 绑定 `HotkeySettings.*Hotkey.Key`。
- 软件内热键不经过系统级注册，焦点窗口内生效。

### 托盘状态联动
- `HotkeySettings.UpdateTrayIconWithPriority()` 优先级：
  1. `DisableGlobalHotkeys` -> `NoHotkey` 图标
  2. `IgnoreHotkeysOnFullscreen` -> `IgnoreOnFullScreen` 图标
  3. 默认 -> 正常图标

## 关键数据结构/配置
- `HotkeySettings.RegisteredHotkeys`：统一热键定义清单与适用窗口类型。
- `HotkeyType`：`Global/MainWindow/SettingsWindow/OcrWindow/ImageTransWindow`。
- `GlobalHotkey.IsConflict`：注册冲突状态。
- 触发策略配置：
  - `DisableGlobalHotkeys`
  - `IgnoreHotkeysOnFullscreen`
  - `CrosswordTranslateByCtrlSameC`
  - `IncrementalTranslateKey`

## 关键文件
- `STranslate/Core/HotkeySettings.cs`
- `STranslate/Helpers/HotkeyMapper.cs`
- `STranslate/Helpers/CtrlSameCHelper.cs`
- `STranslate/Helpers/MouseKeyHelper.cs`
- `STranslate/Helpers/ClipboardMonitor.cs`
- `STranslate/ViewModels/MainWindowViewModel.cs`
- `STranslate/Views/MainWindow.xaml`

## 常见改动任务
- 新增全局热键：在 `HotkeySettings` 增加字段、`RegisteredHotkeys` 声明、`HandleGlobalLogic` 映射。
- 新增软件内热键：在对应窗口 XAML `InputBindings` 绑定 `HotkeySettings` 键值。
- 解决热键冲突：优先查看 `GlobalHotkey.IsConflict` 与 `HotkeyMapper.SetHotkey` 异常日志。
- 调整全屏忽略策略：统一改 `HotkeyMapper.ShouldSkipHotkey()` 与 `HotkeySettings.WithFullscreenCheck()`。
