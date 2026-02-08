# 功能特性

## 功能概览

| 功能 | 说明 |
|------|------|
| 全局热键 | 系统级快捷键，即使应用未聚焦也能触发 |
| 软件内热键 | 仅在应用聚焦时生效的快捷键 |
| 按住触发键 | 按住特定键时临时激活功能 |
| 剪贴板监听 | 监听剪贴板变化并自动翻译 |
| 历史记录 | SQLite 存储翻译历史，支持搜索、导出、收藏 |

## 热键系统

热键系统支持**全局热键**（系统级，即使应用未聚焦也能触发）和**软件内热键**（仅在应用聚焦时生效）。

### 热键类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `GlobalHotkey` | 全局热键，通过 NHotkey.Wpf 注册 | 打开窗口、截图翻译、划词翻译等 |
| `Hotkey` | 软件内热键，通过 WPF KeyBinding | 窗口内快捷键如 Ctrl+B 自动翻译 |
| 按住触发键 | 通过低级别键盘钩子实现 | 按住特定键时临时激活功能 |

### 热键数据结构 (`Core/HotkeyModel.cs`)

```csharp
public record struct HotkeyModel
{
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public bool Ctrl { get; set; }
    public Key CharKey { get; set; } = Key.None;

    // 转换为 ModifierKeys 用于 NHotkey 注册
    public readonly ModifierKeys ModifierKeys { get; }

    // 从字符串解析（如 "Ctrl + Alt + T"）
    public HotkeyModel(string hotkeyString)

    // 验证热键有效性
    public bool Validate(bool validateKeyGestrue = false)
}
```

### 热键设置 (`Core/HotkeySettings.cs`)

```csharp
public partial class HotkeySettings : ObservableObject
{
    // 全局热键
    public GlobalHotkey OpenWindowHotkey { get; set; } = new("Alt + G");
    public GlobalHotkey InputTranslateHotkey { get; set; } = new("None");
    public GlobalHotkey CrosswordTranslateHotkey { get; set; } = new("Alt + D");
    public GlobalHotkey ScreenshotTranslateHotkey { get; set; } = new("Alt + S");
    public GlobalHotkey ClipboardMonitorHotkey { get; set; } = new("Alt + Shift + A");  // 剪贴板监听开关
    public GlobalHotkey OcrHotkey { get; set; } = new("Alt + Shift + S");
    // ... 其他全局热键

    // 软件内热键 - MainWindow
    public Hotkey OpenSettingsHotkey { get; set; } = new("Ctrl + OemComma");
    public Hotkey AutoTranslateHotkey { get; set; } = new("Ctrl + B");
    // ... 其他软件内热键
}
```

### 全局热键注册 (`Helpers/HotkeyMapper.cs`)

全局热键注册使用两种机制：

#### 1. NHotkey.Wpf（标准热键）

```csharp
internal static bool SetHotkey(HotkeyModel hotkey, Action action)
{
    HotkeyManager.Current.AddOrReplace(
        hotkeyStr,
        hotkey.CharKey,
        hotkey.ModifierKeys,
        (_, _) => action.Invoke()
    );
}
```

#### 2. ChefKeys（Win 键专用）

```csharp
// LWin/RWin 需要使用 ChefKeys 库
if (hotkeyStr is "LWin" or "RWin")
    return SetWithChefKeys(hotkeyStr, action);
```

#### 3. 低级别键盘钩子（按住触发）

```csharp
// 使用 SetWindowsHookEx(WH_KEYBOARD_LL) 实现全局按键监听
public static void StartGlobalKeyboardMonitoring()
{
    _hookProc = HookCallback;
    _hookHandle = PInvoke.SetWindowsHookEx(
        WINDOWS_HOOK_ID.WH_KEYBOARD_LL,
        _hookProc,
        hModule,
        0
    );
}
```

### 热键注册流程

```
App.OnStartup()
→ _hotkeySettings.LazyInitialize()
   → ApplyCtrlCc()              // 启用/禁用 Ctrl+CC 划词
   → ApplyIncrementalTranslate() // 启用/禁用增量翻译按键
   → RegisterHotkeys()          // 注册所有全局热键
      → HotkeyMapper.SetHotkey() // 每个热键调用 NHotkey
```

### 全屏检测与热键屏蔽

```csharp
private Action WithFullscreenCheck(Action action)
{
    return () =>
    {
        if (settings.IgnoreHotkeysOnFullscreen &&
            Win32Helper.IsForegroundWindowFullscreen())
            return;  // 全屏时忽略热键

        action();
    };
}
```

### 托盘图标状态

热键状态通过托盘图标反映（优先级从高到低）：

| 状态 | 图标 | 说明 |
|------|------|------|
| `NoHotkey` | 禁用热键图标 | 全局热键被禁用 (`DisableGlobalHotkeys=true`) |
| `IgnoreOnFullScreen` | 全屏忽略图标 | 全屏时忽略热键 (`IgnoreHotkeysOnFullscreen=true`) |
| `Normal` | 正常图标 | 热键正常工作 |
| `Dev` | 开发版图标 | Debug 模式下的正常状态 |

### 热键冲突处理

```csharp
// 注册前检查热键是否可用
internal static bool CheckAvailability(HotkeyModel currentHotkey)
{
    try
    {
        HotkeyManager.Current.AddOrReplace("Test", key, modifiers, ...);
        return true;  // 可以注册
    }
    catch
    {
        return false; // 热键被占用
    }
}

// 冲突时标记并提示用户
GlobalHotkey.IsConflict = !HotkeyMapper.SetHotkey(...);
```

### 特殊热键功能

#### 1. Ctrl+CC 划词翻译

- 监听 Ctrl 键状态，检测快速按两次 C 键
- 通过 `CtrlSameCHelper` 实现（使用 `MouseKeyHook` 库）
- 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

#### 2. 按住触发键

- 注册按住键：按下时触发 `OnPress`，抬起时触发 `OnRelease`
- 用于增量翻译等功能
- 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

#### 3. 热键编辑控件 (`Controls/HotkeyControl.cs`)

- 自定义 WPF 控件用于热键设置界面
- 弹出对话框捕获按键输入
- 支持验证和冲突检测

### 相关文件

| 文件 | 用途 |
|------|---------|
| `STranslate/Core/HotkeySettings.cs` | 热键配置模型、热键注册管理 |
| `STranslate/Core/HotkeyModel.cs` | 热键数据结构、解析与验证 |
| `STranslate/Helpers/HotkeyMapper.cs` | 热键注册、低级别键盘钩子 |
| `STranslate/Controls/HotkeyControl.cs` | 热键设置自定义控件 |
| `STranslate/Controls/HotkeyDisplay.cs` | 热键显示自定义控件 |
| `STranslate/Views/Pages/HotkeyPage.xaml` | 热键设置页面 |

## 剪贴板监听功能

剪贴板监听功能允许应用程序在后台监视系统剪贴板的变化，当检测到文本内容时自动触发翻译。

### 实现架构

#### 核心组件 (`Helpers/ClipboardMonitor.cs`)

- 使用 Win32 API `AddClipboardFormatListener` / `RemoveClipboardFormatListener` 注册剪贴板监听
- 通过 `HwndSource` 在 WPF 窗口上挂接 `WndProc` 接收 `WM_CLIPBOARDUPDATE` 消息
- 使用 CsWin32 PInvoke 生成类型安全的 Win32 API 绑定

```csharp
public class ClipboardMonitor : IDisposable
{
    private HwndSource? _hwndSource;
    private HWND _hwnd;
    private string _lastText = string.Empty;

    public event Action<string>? OnClipboardTextChanged;

    public void Start()
    {
        // 使用 WindowInteropHelper 获取窗口句柄
        var windowHelper = new WindowInteropHelper(_window);
        _hwnd = new HWND(windowHelper.Handle);
        _hwndSource = HwndSource.FromHwnd(windowHelper.Handle);
        _hwndSource?.AddHook(WndProc);
        PInvoke.AddClipboardFormatListener(_hwnd);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == PInvoke.WM_CLIPBOARDUPDATE)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);  // 延迟确保剪贴板数据已完全写入
                var text = ClipboardHelper.GetText();
                if (!string.IsNullOrWhiteSpace(text) && text != _lastText)
                {
                    _lastText = text;
                    OnClipboardTextChanged?.Invoke(text);
                    _lastText = string.Empty;  // 触发后重置，允许相同内容再次触发
                }
            });
            handled = true;
        }
        return nint.Zero;
    }
}
```

### 控制方式

1. **全局热键**: `Alt + Shift + A`（默认）- 在任何地方切换监听状态
2. **主窗口按钮**: HeaderControl 中的切换按钮，带状态指示（IsOn/IsOff）
3. **设置项**: `Settings.IsClipboardMonitorVisible` 控制按钮是否显示

### 状态通知

开启/关闭状态通过 Windows 托盘通知（Toast Notification）提示用户，因为此时主窗口可能处于隐藏状态。

### 实现细节

#### 延迟处理

使用 `await Task.Delay(100)` 延迟 100ms 确保剪贴板数据已完全写入，避免读取到空或不完整的数据。

#### 重复触发处理

- 使用 `_lastText` 字段记录上一次触发内容
- 触发后重置为空字符串，允许相同内容再次触发（用户可能再次复制相同内容）

#### 线程安全

剪贴板操作在后台线程执行，避免阻塞 UI 线程：

```csharp
_ = Task.Run(async () =>
{
    // 剪贴板操作
});
```

### 相关文件

| 文件 | 用途 |
|------|---------|
| `STranslate/Helpers/ClipboardMonitor.cs` | 剪贴板监听实现（Win32 API） |
| `STranslate/Views/HeaderControl.xaml` | 主窗口标题栏按钮 |

## 历史记录功能

历史记录功能用于保存和管理用户的翻译历史，支持搜索、导出、删除和收藏等功能。

### 功能概述

- **数据存储**: SQLite 数据库存储历史记录
- **分页加载**: 游标分页实现懒加载，优化大数据量性能
- **搜索功能**: 支持按内容模糊搜索
- **导出功能**: 支持将选中记录导出为 JSON 文件
- **批量删除**: 支持多选记录批量删除
- **收藏功能**: 支持标记常用翻译记录

### 数据模型

#### HistoryModel

历史记录数据模型，存储在 SQLite 数据库中：

```csharp
public class HistoryModel
{
    public long Id { get; set; }              // 唯一标识
    public DateTime Time { get; set; }        // 记录时间
    public string SourceLang { get; set; }    // 源语言
    public string TargetLang { get; set; }    // 目标语言
    public string SourceText { get; set; }    // 源文本
    public bool Favorite { get; set; }        // 是否收藏
    public string Remark { get; set; }        // 备注
    public List<HistoryData> Data { get; set; } // 翻译结果数据
}
```

#### HistoryListItem

视图层包装类，用于 UI 绑定：

```csharp
public partial class HistoryListItem : ObservableObject
{
    public HistoryModel Model { get; }        // 底层数据模型
    public bool IsExportSelected { get; set; } // 是否选中（用于批量导出/删除）
}
```

### 核心功能

#### 分页加载

使用游标分页（Cursor Pagination）实现高效的大数据量加载：

```csharp
private const int PageSize = 20;
private DateTime _lastCursorTime = DateTime.Now;

[RelayCommand(CanExecute = nameof(CanLoadMore))]
private async Task LoadMoreAsync()
{
    var historyData = await _sqlService.GetDataCursorPagedAsync(PageSize, _lastCursorTime);
    if (!historyData.Any()) return;

    // 更新游标为最后一条记录的时间
    _lastCursorTime = historyData.Last().Time;
    // 添加数据到列表
    AddItems(historyData);
}
```

#### 搜索功能

支持模糊搜索，带防抖处理：

```csharp
private const int searchDelayMilliseconds = 500;
private readonly DebounceExecutor _searchDebouncer;

// 搜索文本变化时触发防抖搜索
partial void OnSearchTextChanged(string value) =>
    _searchDebouncer.ExecuteAsync(SearchAsync, TimeSpan.FromMilliseconds(searchDelayMilliseconds));

private async Task SearchAsync()
{
    var historyItems = await _sqlService.GetDataAsync(SearchText, _searchCts.Token);
    // 更新列表
}
```

#### 导出功能

支持将选中的历史记录导出为 JSON 文件：

```csharp
[RelayCommand]
private async Task ExportHistoryAsync()
{
    var selected = _items
        .Where(i => i.IsExportSelected)
        .Select(i => i.Model)
        .ToList();

    var export = new
    {
        app = Constant.AppName,
        exportedAt = DateTimeOffset.Now,
        count = selected.Count,
        items = selected.Select(h => new { ... })
    };

    var json = JsonSerializer.Serialize(export, HistoryModel.JsonOption);
    await File.WriteAllTextAsync(saveFileDialog.FileName, json, Encoding.UTF8);
}
```

#### 批量删除

支持多选后批量删除：

```csharp
[RelayCommand]
private async Task DeleteSelectedHistoryAsync()
{
    var selected = _items.Where(i => i.IsExportSelected).ToList();
    // 显示确认对话框
    // 逐个删除选中的记录
    foreach (var item in selected)
        await DeleteAsync(item.Model);
}
```

### 分页机制

#### 游标分页优势

相比传统的 Offset 分页，游标分页具有以下优势：
- **性能稳定**: 不受数据量增长影响，始终 O(1) 查询
- **无数据重复/遗漏**: 插入新数据不会影响现有分页
- **适合时间序列数据**: 按时间倒序排列的场景

#### 分页状态管理

```csharp
private bool CanLoadMore =>
    !_isLoading &&                           // 不在加载中
    string.IsNullOrEmpty(SearchText) &&      // 非搜索模式
    (TotalCount == 0 || _items.Count != TotalCount); // 还有数据未加载
```

### 相关文件

| 文件 | 用途 |
|------|------|
| `STranslate/ViewModels/Pages/HistoryViewModel.cs` | 历史记录页面 ViewModel |
| `STranslate/Views/Pages/HistoryPage.xaml` | 历史记录页面 UI |
| `STranslate/Models/HistoryModel.cs` | 历史记录数据模型 |
| `STranslate/Services/SqlService.cs` | SQLite 数据库服务 |
