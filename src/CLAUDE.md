# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 在处理本仓库代码时提供指导。

## 项目概述

**STranslate** 是一个基于 Windows WPF 的翻译和 OCR 工具，采用插件化架构。它通过可扩展的插件支持多种翻译服务、OCR 提供商、TTS（文本转语音）和词汇管理。

### 主要功能

- **翻译服务**: 支持多种翻译引擎（内置 + 插件扩展）
- **OCR 文字识别**: 截图识别、静默识别
- **剪贴板监听**: 监听剪贴板变化并自动翻译（可通过全局热键 `Alt + Shift + A` 或主窗口按钮切换）
- **划词翻译**: 鼠标选中文本后通过热键翻译
- **截图翻译**: 截取屏幕区域进行 OCR 和翻译
- **TTS 朗读**: 文本转语音
- **生词本**: 保存和复习翻译过的单词

## 构建与开发命令

### 构建命令
```powershell
# 构建 Debug 配置
dotnet build STranslate.sln --configuration Debug

# 构建 Release 配置
dotnet build STranslate.sln --configuration Release

# 构建特定版本（build.ps1 使用）
dotnet build STranslate.sln --configuration Release /p:Version=2.0.0

# 运行构建脚本（清理、更新版本、构建、清理）
./build.ps1 -Version "2.0.0"
```

### 运行应用程序
```powershell
# 运行 Debug 构建
dotnet run --project STranslate/STranslate.csproj

# 或构建后直接运行可执行文件
./.artifacts/Debug/STranslate.exe
```

### 项目结构
```
├── STranslate/                    # 主 WPF 应用程序
│   ├── Core/                     # 核心服务（PluginManager, ServiceManager 等）
│   ├── Services/                 # 应用程序服务（TranslateService, OcrService 等）
│   ├── ViewModels/               # MVVM ViewModels
│   ├── Views/                    # WPF Views/Pages
│   ├── Controls/                 # 自定义 WPF 控件
│   ├── Converters/               # 值转换器
│   └── Plugin/                   # 插件接口定义（共享）
├── STranslate.Plugin/            # 共享插件接口和模型
└── Plugins/                      # 插件实现
    ├── STranslate.Plugin.Translate.*      # 官方内置翻译插件
    ├── STranslate.Plugin.Ocr.*            # 官方内置 OCR 插件
    ├── STranslate.Plugin.Tts.*            # 官方内置 TTS 插件
    ├── STranslate.Plugin.Vocabulary.*     # 官方内置词汇插件
    └── ThirdPlugins/                      # 社区/第三方插件
        ├── STranslate.Plugin.Translate.DeepLX/      # DeepLX 翻译插件
        ├── STranslate.Plugin.Translate.Gemini/      # Gemini 翻译插件
        ├── STranslate.Plugin.Translate.Ali/         # 阿里云翻译插件
        ├── STranslate.Plugin.Translate.QwenMt/      # 通义千问翻译插件
        ├── STranslate.Plugin.Translate.GoogleWebsite/ # Google 网页翻译插件
        ├── STranslate.Plugin.Translate.BingDict/    # 必应词典插件
        ├── STranslate.Plugin.Ocr.Gemini/            # Gemini OCR 插件
        ├── STranslate.Plugin.Ocr.Paddle/            # Paddle OCR 插件
        └── STranslate.Plugin.Vocabulary.Maimemo/    # 默默记单词生词本插件
```

#### 社区插件 (ThirdPlugins) 说明

`ThirdPlugins` 目录用于存放**社区贡献的第三方插件**，这些插件通常：
- 由社区开发者独立维护
- 每个插件是一个**独立的 Git 仓库**（子模块或独立项目）
- 具有独立的版本号和发布周期
- 可以独立开发、测试和发布

**推荐的插件目录结构（每个插件独立仓库）：**
```
STranslate.Plugin.Translate.DeepLX/
├── .git/                          # 独立 Git 仓库
├── README.md                      # 插件说明文档
├── CHANGELOG.md                   # 更新日志
├── LICENSE                        # 开源许可证
├── STranslate.Plugin.Translate.DeepLX/
│   ├── STranslate.Plugin.Translate.DeepLX.csproj  # 项目文件
│   ├── Main.cs                      # 插件主类（实现 TranslatePluginBase）
│   ├── Settings.cs                  # 配置模型类
│   ├── plugin.json                  # 插件元数据
│   ├── icon.png                     # 插件图标
│   ├── Languages/                   # 多语言文件
│   │   ├── zh-cn.xaml
│   │   ├── en.xaml
│   │   └── ...
│   ├── View/                        # 设置界面 XAML
│   │   ├── SettingsView.xaml
│   │   └── SettingsView.xaml.cs
│   └── ViewModel/                   # 设置界面 ViewModel
│       └── SettingsViewModel.cs
├── .artifacts/                     # 编译输出（可选）
└── obj/                            # 编译中间文件（gitignore）
```

## 架构

### 核心架构流程

1. **应用程序启动** (`App.xaml.cs:296-319`)
   - 通过 `SingleInstance<App>` 强制单实例
   - Velopack 更新检查
   - 设置加载（Settings, HotkeySettings, ServiceSettings）
   - DI 容器设置（Microsoft.Extensions.Hosting）
   - 通过 `PluginManager` 加载插件
   - 通过 `ServiceManager` 初始化服务

2. **插件系统** (`Core/PluginManager.cs`)
   - 插件从两个目录加载：
     - `PreinstalledDirectory`: 内置插件，在 `Plugins/` 文件夹
     - `PluginsDirectory`: 用户安装的插件，在数据目录
   - 每个插件是一个 `.spkg` 文件（ZIP 压缩包），包含：
     - `plugin.json` - 元数据
     - 插件 DLL
     - 可选资源（图标、语言文件）
   - 插件实现 `IPlugin` 接口及其子类型：
     - `ITranslatePlugin` - 翻译服务
     - `IOcrPlugin` - OCR 服务
     - `ITtsPlugin` - 文本转语音
     - `IVocabularyPlugin` - 词汇管理
     - `IDictionaryPlugin` - 字典查询

3. **服务管理** (`Core/ServiceManager.cs`)
   - `Service` 是包装插件的运行时实例
   - 服务从 `PluginMetaData` 创建并存储在 `ServiceData`（持久化配置）中
   - 四种服务类型：翻译、OCR、TTS、词汇
   - 服务在启动时从设置和插件元数据加载

4. **插件生命周期**
   ```
   PluginManager.LoadPlugins()
   → 扫描插件目录
   → 从 plugin.json 提取元数据
   → 通过 PluginAssemblyLoader 加载程序集
   → 查找 IPlugin 实现
   → 创建带类型信息的 PluginMetaData

   ServiceManager.LoadServices()
   → 遍历设置中的每个 ServiceData
   → 与 PluginMetaData 匹配
   → 创建 Service 实例
   → 调用 Service.Initialize() → Plugin.Init(IPluginContext)
   ```

### 关键接口

**IPlugin** (基础接口)
- `Init(IPluginContext context)` - 使用上下文初始化
- `GetSettingUI()` - 返回 UserControl 用于设置
- `Dispose()` - 清理

**IPluginContext** (提供给插件)
- `MetaData`, `Logger`, `HttpService`, `AudioPlayer`, `Snackbar`, `Notification`
- `LoadSettingStorage<T>()` / `SaveSettingStorage<T>()` - 持久化存储（自动定位到插件专属目录）
- `GetTranslation(key)` - i18n 支持

**ITranslatePlugin** (翻译插件)
- `TranslateAsync(TranslateRequest, TranslateResult)` - 核心翻译
- `GetSourceLanguage()` / `GetTargetLanguage()` - 语言映射
- `TransResult` / `TransBackResult` - 结果属性

**IOcrPlugin** (OCR 插件)
- `RecognizeAsync(OcrRequest)` - 图像转文本
- `SupportedLanguages` - 可用语言

### Service 与 Plugin 的关系

理解 **Service** 和 **Plugin** 的区别至关重要：

- **Plugin (`PluginMetaData`)**: 插件的类型定义，包含程序集信息、元数据。同一插件类型可被多个 Service 共享使用。
- **Service**: 插件的运行时实例，拥有独立的配置、状态和服务 ID (`ServiceID`)。多个 Service 可使用同一 Plugin 类型（如两个不同 API Key 的百度翻译服务）。

Service 创建流程（`ServiceManager.CreateService()`）：
1. 克隆 `PluginMetaData`（每个 Service 有自己的副本）
2. 创建或重用 `ServiceID`（GUID）
3. 通过 `Activator.CreateInstance()` 创建插件实例
4. 创建 `PluginContext` 提供给插件
5. 组装 `Service` 对象，包含 `Plugin`、`MetaData`、`Context` 和 `Options`

### 热键系统

热键系统支持**全局热键**（系统级，即使应用未聚焦也能触发）和**软件内热键**（仅在应用聚焦时生效）。

#### 热键类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `GlobalHotkey` | 全局热键，通过 NHotkey.Wpf 注册 | 打开窗口、截图翻译、划词翻译等 |
| `Hotkey` | 软件内热键，通过 WPF KeyBinding | 窗口内快捷键如 Ctrl+B 自动翻译 |
| 按住触发键 | 通过低级别键盘钩子实现 | 按住特定键时临时激活功能 |

#### 热键数据结构 (`Core/HotkeyModel.cs`)

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

#### 热键设置 (`Core/HotkeySettings.cs`)

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

#### 全局热键注册 (`Helpers/HotkeyMapper.cs`)

全局热键注册使用两种机制：

1. **NHotkey.Wpf**（标准热键）：
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

2. **ChefKeys**（Win 键专用）：
   ```csharp
   // LWin/RWin 需要使用 ChefKeys 库
   if (hotkeyStr is "LWin" or "RWin")
       return SetWithChefKeys(hotkeyStr, action);
   ```

3. **低级别键盘钩子**（按住触发）：
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

#### 热键注册流程

```
App.OnStartup()
→ _hotkeySettings.LazyInitialize()
   → ApplyCtrlCc()              // 启用/禁用 Ctrl+CC 划词
   → ApplyIncrementalTranslate() // 启用/禁用增量翻译按键
   → RegisterHotkeys()          // 注册所有全局热键
      → HotkeyMapper.SetHotkey() // 每个热键调用 NHotkey
```

#### 全屏检测与热键屏蔽

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

#### 托盘图标状态

热键状态通过托盘图标反映（优先级从高到低）：

| 状态 | 图标 | 说明 |
|------|------|------|
| `NoHotkey` | 禁用热键图标 | 全局热键被禁用 (`DisableGlobalHotkeys=true`) |
| `IgnoreOnFullScreen` | 全屏忽略图标 | 全屏时忽略热键 (`IgnoreHotkeysOnFullscreen=true`) |
| `Normal` | 正常图标 | 热键正常工作 |
| `Dev` | 开发版图标 | Debug 模式下的正常状态 |

#### 热键冲突处理

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

#### 特殊热键功能

1. **Ctrl+CC 划词翻译**：
   - 监听 Ctrl 键状态，检测快速按两次 C 键
   - 通过 `CtrlSameCHelper` 实现（使用 `MouseKeyHook` 库）
   - 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

2. **按住触发键**：
   - 注册按住键：按下时触发 `OnPress`，抬起时触发 `OnRelease`
   - 用于增量翻译等功能
   - 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

3. **热键编辑控件** (`Controls/HotkeyControl.cs`)：
   - 自定义 WPF 控件用于热键设置界面
   - 弹出对话框捕获按键输入
   - 支持验证和冲突检测

### 剪贴板监听功能

剪贴板监听功能允许应用程序在后台监视系统剪贴板的变化，当检测到文本内容时自动触发翻译。

#### 实现架构

**核心组件** (`Helpers/ClipboardMonitor.cs`):
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

#### 控制方式

1. **全局热键**: `Alt + Shift + A`（默认）- 在任何地方切换监听状态
2. **主窗口按钮**: HeaderControl 中的切换按钮，带状态指示（IsOn/IsOff）
3. **设置项**: `Settings.IsClipboardMonitorVisible` 控制按钮是否显示

#### 状态通知

开启/关闭状态通过 Windows 托盘通知（Toast Notification）提示用户，因为此时主窗口可能处于隐藏状态。

### 数据流：翻译示例

1. **用户触发翻译**（快捷键、UI）→ `MainWindowViewModel.TranslateCommand`
2. **翻译准备**：
   - 取消进行中的操作
   - 重置所有服务状态（`ResetAllServices()`）
   - 语言检测（`LanguageDetector.GetLanguageAsync()`）
3. **获取激活服务**：`TranslateService` 返回 `ExecutionMode.Automatic` 的启用服务
4. **并行执行**（`ExecuteTranslationForServicesAsync`）：
   - 使用 `SemaphoreSlim` 限制并发数（`ProcessorCount * 10`）
   - 对每个 Service 调用 `ExecuteTranslationHandlerAsync`
5. **插件执行**（`ExecuteAsync`）：
   ```csharp
   plugin.Reset();
   plugin.TransResult.IsProcessing = true;
   await plugin.TranslateAsync(
       new TranslateRequest(InputText, source, target),
       plugin.TransResult,
       cancellationToken
   );
   ```
6. **插件内部**：
   - 使用 `Context.LoadSettingStorage<Settings>()` 获取 API 密钥等配置
   - 使用 `Context.HttpService` 发起 HTTP 请求（支持代理）
   - 解析响应，调用 `result.Success(text)` 或 `result.Fail(message)`
7. **结果处理**：
   - `TranslateResult` 是 `ObservableObject`，自动更新 UI
   - 如启用回译，调用 `ExecuteBackAsync()`
   - 保存到历史数据库（`SqlService`）

### 设置与存储

**设置架构** (`StorageBase<T>`)
- JSON 序列化，原子写入（`.tmp` + `.bak` 备份）
- 位于 `DataLocation.DataDirectory()`：
  - 便携模式：`./PortableConfig/`
  - 漫游模式：`%APPDATA%\STranslate\`

**主要设置文件**
- `Settings.json` - 通用设置
- `HotkeySettings.json` - 快捷键配置
- `ServiceSettings.json` - 服务配置（启用、顺序、选项）

**插件存储**
- 设置：`%APPDATA%\STranslate\Settings\Plugins\{PluginName}_{PluginID}\{ServiceID}.json`
- 缓存：`%APPDATA%\STranslate\Cache\Plugins\{PluginName}_{PluginID}\`
- 通过 `IPluginContext.LoadSettingStorage<T>()` / `SaveSettingStorage<T>()` 访问，无需关心具体路径

### 插件包格式 (.spkg)

`.spkg` 文件是 ZIP 压缩包，包含：
```
plugin.json          # 元数据
YourPlugin.dll       # 主程序集
icon.png            # 可选图标
Languages/*.xaml     # 可选 i18n 文件
```

**plugin.json 示例:**
```json
{
  "PluginID": "unique-id",
  "Name": "Plugin Name",
  "Author": "Author",
  "Version": "1.0.0",
  "Description": "Description",
  "Website": "https://example.com",
  "ExecuteFileName": "YourPlugin.dll",
  "IconPath": "icon.png"
}
```

## 常见开发任务

### 添加新的插件类型
1. 在 `STranslate.Plugin/` 中定义接口（例如 `IMyPlugin.cs`）
2. 添加到 `ServiceType` 枚举（如果是新的服务类别）
3. 更新 `BaseService.LoadPlugins<T>()` 以加载该类型
4. 更新 `ServiceManager.CreateService()` 以处理该类型
5. 在 `Services/` 中创建服务类（例如 `MyService.cs`）

### 修改核心服务
- **TranslateService**: `STranslate/Services/TranslateService.cs`
- **OcrService**: `STranslate/Services/OcrService.cs`
- **TtsService**: `STranslate/Services/TtsService.cs`
- **VocabularyService**: `STranslate/Services/VocabularyService.cs`

### UI 更改
- Views 在 `STranslate/Views/`
- ViewModels 在 `STranslate/ViewModels/`
- 使用 CommunityToolkit.Mvvm 进行 MVVM
- 使用 iNKORE.UI.WPF.Modern 用于现代 UI 组件

### 调试插件加载
检查日志文件 `%APPDATA%\STranslate\Logs\{Version}\.log`：
- 插件发现
- 程序集加载
- 类型解析
- 初始化错误

### 测试插件安装
1. 构建插件为 `.spkg`（带 plugin.json 的 ZIP）
2. 使用 UI：设置 → 插件 → 安装
3. 或放在 `Plugins/` 目录作为预安装插件

## 社区插件开发指南 (ThirdPlugins)

### 标准开发流程

**核心原则：基于现有插件仓库修改**

1. **找到官方社区插件仓库**
   - 访问 GitHub: `https://github.com/STranslate/STranslate.Plugin.Translate.{Name}`
   - 选择与你需求相似的插件类型（翻译/OCR/TTS/生词本）

2. **Clone 仓库**
   ```bash
   # Clone 代码
   git clone https://github.com/STranslate/STranslate.Plugin.Translate.DeepLX.git
   cd STranslate.Plugin.Translate.DeepLX
   
   # 创建自己的 git 仓库
   rm -rf .git
   git init .

   # 重命名操作

   ```

3. **修改必要信息**
   - `plugin.json`: 更新 PluginID、Name、Version、Description
   - `*.csproj`: 更新项目名称、RepositoryUrl
   - `Main.cs`: 修改核心逻辑
   - `Settings.cs`: 修改配置模型
   - `icon.png`: 替换图标

   **在主项目中调试（可断点调试）：**
   ```powershell
   # 1. 下载主项目代码
   git clone https://github.com/STranslate/STranslate.git
   cd STranslate

   # 2. 将插件代码放到 Plugins/ThirdPlugins/ 目录
   #    例如：Plugins/ThirdPlugins/STranslate.Plugin.Translate.YourPlugin/

   # 3. 添加到解决方案
   dotnet sln add Plugins/ThirdPlugins/STranslate.Plugin.Translate.YourPlugin/STranslate.Plugin.Translate.YourPlugin/STranslate.Plugin.Translate.YourPlugin.csproj

   # 4. 在 Visual Studio 中
   #    - 打开 STranslate.sln
   #    - 设置 STranslate 为启动项目
   #    - 配置 Debug 模式
   #    - 右键插件项目 → 编译
   #    - 启动调试（F5）
   #    - 插件会自动加载，可设置断点
   ```

5. **版本管理与发布**
   ```bash
   # 1. 更新版本号（plugin.json）
   # 2. 更新 CHANGELOG.md
   # 3. 提交代码
   git add .
   git commit -m "feat: add your feature"

   # 4. 打 tag（注意：tag 必须以 v 开头，后面跟版本号）
   git tag v1.0.0
   git push origin main
   git push origin v1.0.0

   # 5. GitHub Actions 自动构建并发布 Release
   #    - 生成 .spkg 文件
   #    - 创建 Release
   #    - 上传 .spkg 作为附件
   ```

### 版本号与 Tag 规范

**重要规则：**
- `plugin.json` 中的 `"Version"` 必须与 Git Tag 一致
- Tag 格式：`v{版本号}`，例如：`v1.0.0`、`v1.2.3`
- 版本号更新后必须打新 Tag 才能触发自动发布

**示例：**
```json
// plugin.json
{
  "Version": "1.0.0",  // ← 这个版本号
  ...
}
```

```bash
# 对应的 Tag
git tag v1.0.0  # ← 必须以 v 开头
git push origin v1.0.0
```

### 插件类型参考表

| 插件类型 | 基类 | 主要接口 | 说明 |
|---------|------|---------|------|
| 翻译 | `TranslatePluginBase` / `LlmTranslatePluginBase` | `ITranslatePlugin` | 支持 LLM 的使用 `LlmTranslatePluginBase` |
| OCR | `OcrPluginBase` | `IOcrPlugin` | 图像识别 |
| TTS | `TtsPluginBase` | `ITtsPlugin` | 文本转语音 |
| 词汇 | `VocabularyPluginBase` | `IVocabularyPlugin` | 单词查询/管理 |

**插件基类说明**：
- `TranslatePluginBase`: 提供常用功能如语言映射、结果处理、HTTP 请求辅助
- `LlmTranslatePluginBase`: 继承自 `TranslatePluginBase`，专为 LLM 服务设计，提供提示词编辑、流式响应处理
- 插件基类位于 `STranslate.Plugin` 项目中，插件项目需引用该包

### 已知社区插件示例

| 插件名称 | 类型 | 仓库地址 |
|---------|------|---------|
| DeepLX | 翻译 | `STranslate/STranslate.Plugin.Translate.DeepLX` |
| Gemini | 翻译 | `STranslate/STranslate.Plugin.Translate.Gemini` |
| 阿里云翻译 | 翻译 | `STranslate/STranslate.Plugin.Translate.Ali` |
| 通义千问 | 翻译 | `STranslate/STranslate.Plugin.Translate.QwenMt` |
| Google 网页翻译 | 翻译 | `STranslate/STranslate.Plugin.Translate.GoogleWebsite` |
| 必应词典 | 翻译 | `STranslate/STranslate.Plugin.Translate.BingDict` |
| Gemini OCR | OCR | `STranslate/STranslate.Plugin.Ocr.Gemini` |
| Paddle OCR | OCR | `STranslate/STranslate.Plugin.Ocr.Paddle` |
| 默默记单词生词本 | 词汇 | `STranslate/STranslate.Plugin.Vocabulary.Maimemo` |

### 常见问题

**Q: 如何获取插件的唯一 ID？**
A: 使用 GUID 生成器创建一个 32 位十六进制字符串，例如：`d99c702e39b44be5a9e49983ff0f4fff`，每个插件都需要重新生成，唯一ID重复可能会导致插件无法使用

**Q: 插件需要哪些必备文件？**
A: `plugin.json`, `Main.cs`, `Settings.cs`, `icon.png`, `.csproj`

**Q: 如何调试插件？**
A:
1. 设置 Debug 输出路径到主程序的 Plugins 目录
2. 启动主程序，插件会自动加载
3. 查看 `%APPDATA%\STranslate\Logs\` 中的日志

**Q: 插件版本如何管理？**
A:
1. 在 `plugin.json` 中更新 `Version`
2. 更新 `CHANGELOG.md`
3. 打 Tag: `git tag v1.0.0`
4. 推送: `git push origin main && git push origin v1.0.0`
5. GitHub Actions 自动构建并发布 Release

**Q: 如何支持多语言？**
A: 在 `Languages/` 目录添加 `.xaml` 和 `.json` 文件，通过 `IPluginContext.GetTranslation()` 获取

## 重要文件

| 文件 | 用途 |
|------|---------|
| `STranslate/App.xaml.cs` | 应用程序入口、DI 设置、生命周期 |
| `STranslate/Core/PluginManager.cs` | 插件发现、加载、安装 |
| `STranslate/Core/ServiceManager.cs` | 服务创建、生命周期 |
| `STranslate/Services/BaseService.cs` | 所有服务类型的基础 |
| `STranslate.Plugin/IPlugin.cs` | 核心插件接口 |
| `STranslate.Plugin/PluginMetaData.cs` | 插件元数据模型 |
| `STranslate.Plugin/Service.cs` | 运行时服务实例 |
| `STranslate/Core/HotkeySettings.cs` | 热键配置模型、热键注册管理 |
| `STranslate/Core/HotkeyModel.cs` | 热键数据结构、解析与验证 |
| `STranslate/Helpers/HotkeyMapper.cs` | 热键注册、低级别键盘钩子 |
| `STranslate/Controls/HotkeyControl.cs` | 热键设置自定义控件 |
| `STranslate/Controls/HotkeyDisplay.cs` | 热键显示自定义控件 |
| `STranslate/Views/Pages/HotkeyPage.xaml` | 热键设置页面 |
| `STranslate/Helpers/ClipboardMonitor.cs` | 剪贴板监听实现（Win32 API） |
| `build.ps1` | Release 构建脚本 |
| `Directory.Packages.props` | 集中式 NuGet 版本 |

## 关键依赖

- **WPF 框架**: .NET 10.0-windows
- **MVVM**: CommunityToolkit.Mvvm
- **UI**: iNKORE.UI.WPF.Modern（现代控件/主题）
- **DI**: Microsoft.Extensions.*
- **日志**: Serilog
- **快捷键**: NHotkey.Wpf, MouseKeyHook
- **HTTP**: System.Net.Http（支持代理）
- **存储**: Microsoft.Data.Sqlite（历史数据库位于 `%APPDATA%\STranslate\Cache\history.db`）
- **更新**: Velopack
- **插件加载**: System.Reflection.MetadataLoadContext
- **IL 织入**: Costura.Fody（程序集合并）, MethodBoundaryAspect.Fody

## 给 Claude 的注意事项

- 这是一个 **仅限 Windows 的 WPF 应用程序**（使用 Windows 特定 API）
- 插件在运行时从单独的 DLL **动态加载**
- 所有插件接口都在 `STranslate.Plugin` 项目中，与主应用程序共享
- 设置使用**原子写入**和备份文件
- 应用程序支持**便携模式**（创建 `PortableConfig/` 文件夹）
- 预安装插件在 `Plugins/` 并复制到输出
- 用户插件位于 `%APPDATA%\STranslate\Plugins\`
- 插件实例**按服务创建**（非单例）
- 使用 `IPluginContext` 获取插件功能（不要直接传递应用程序服务）
- 预安装插件 ID 定义在 `Constant.cs:56-74`
- **线程安全**：翻译请求使用 `SemaphoreSlim` 控制并发（默认 `ProcessorCount * 10`），所有插件操作支持 `CancellationToken` 取消
- 插件程序集加载使用 `PluginAssemblyLoader` 和 `System.Reflection.MetadataLoadContext`
- 服务被包装在 `Service` 类中，包含 `Plugin`, `MetaData`, `Context` 和 `Options`
- 翻译插件可以扩展 `TranslatePluginBase` 或 `LlmTranslatePluginBase` 以获得 LLM 功能
- 应用程序使用 Fody 织入器（Costura.Fody 用于程序集合并，MethodBoundaryAspect.Fody 用于 AOP）
- **剪贴板监听**: 使用 Win32 API `AddClipboardFormatListener`，监听状态通过 Windows Toast 通知反馈
