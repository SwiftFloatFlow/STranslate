# 全局提示词功能文档

## 概述

全局提示词（Global Prompts）是 STranslate 的一项高级功能，允许用户创建可在多个 AI 翻译服务间共享的提示词模板。这解决了重复配置相同提示词的问题，提供了统一的提示词管理体验。

## 功能特性

- **集中管理**: 在一个地方创建和维护提示词，多处使用
- **自动同步**: 修改全局提示词后，所有引用的服务自动更新
- **状态保持**: 每个服务可以独立控制全局提示词的启用/禁用状态
- **线程安全**: 完整的并发控制，支持多线程环境下的安全操作
- **向后兼容**: 不影响现有插件和服务的正常运行
- **独立编辑**: 专用的编辑窗口，不依赖插件

## 用户界面

### 入口位置

全局提示词编辑入口位于 **设置窗口 → 服务 → 文本翻译** 页面的右上角工具栏：

```
全局提示词：[编辑] | 图片翻译：百度翻译 [删除] | 替换翻译：xxx [删除]
```

点击 **[✎ 编辑]** 按钮即可打开全局提示词编辑窗口。

### 编辑窗口布局

编辑窗口采用与局部提示词编辑窗口相同的三栏布局：

```
┌────────────────────────────────────────────────────────────┐
│                    全局提示词 - 编辑窗口                     │
├──────────────┬─────────────────────────────────────────────┤
│ 全局提示词列表 │              提示词详情                      │
│              │                                             │
│ [添加][复制]  │  提示项管理 [添加项目] [删除项目]             │
│ [删除]       │                                             │
│              │  ┌────────┐  ┌───────────────────────────┐  │
│ □ 新提示词   │  │system  │  │ 角色: [system ▼]          │  │
│ ☑ 专业翻译   │  │user    │  │                           │  │
│ □ 术语翻译   │  │assistant│  │ ┌─────────────────────┐   │  │
│              │  │        │  │ │ 变量说明:           │   │  │
│              │  │        │  │ │ $source - 源语言    │   │  │
│              │  │        │  │ │ $target - 目标语言  │   │  │
│              │  │        │  │ │ $content - 翻译内容 │   │  │
│              │  │        │  │ └─────────────────────┘   │  │
│              │  │        │  │                           │  │
│              │  │        │  │ ┌─────────────────────┐   │  │
│              │  └────────┘  │ │ 内容编辑区...       │   │  │
│              │              │ │                     │   │  │
│              │              │ └─────────────────────┘   │  │
├──────────────┴─────────────────────────────────────────────┤
│                                    [保存] [取消]            │
└────────────────────────────────────────────────────────────┘
```

### 操作说明

| 操作 | 说明 |
|------|------|
| **添加** | 创建新的全局提示词 |
| **复制** | 复制选中的提示词（创建副本） |
| **删除** | 直接删除选中的提示词（无确认） |
| **开关** | 控制提示词的启用/禁用状态 |
| **添加项目** | 为选中提示词添加新的角色项 |
| **删除项目** | 删除选中的角色项 |
| **保存** | 保存所有更改并关闭窗口 |
| **取消** | 关闭窗口（已自动保存的更改不会撤销） |

## 架构设计

### 核心组件

```
┌─────────────────────────────────────────────────────────────┐
│                     GlobalPromptManager                      │
│  ┌─────────────────┐    ┌─────────────────────────────────┐ │
│  │ Settings        │────│ GlobalPrompts (ObservableCollection)│
│  │                 │    └─────────────────────────────────┘ │
│  └─────────────────┘                                         │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              ServicePromptMerger                         │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │ │
│  │  │  Merge   │  │ ReMerge  │  │ TryFilterGlobalPrompts│  │ │
│  │  └──────────┘  └──────────┘  └──────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────┘ │
│           │                                                  │
│           ▼                                                  │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │              AI Translation Services                     │ │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │ │
│  │  │  Service 1  │  │  Service 2  │  │  Service N  │     │ │
│  │  │  (OpenAI)   │  │  (Gemini)   │  │   (...)     │     │ │
│  │  └─────────────┘  └─────────────┘  └─────────────┘     │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 数据流

1. **创建全局提示词**: 用户在编辑窗口创建 → 添加到 `Settings.GlobalPrompts`
2. **引用全局提示词**: 服务配置中 `ReferencedGlobalPromptIds` 添加 ID
3. **合并到服务**: `GlobalPromptManager` 监听变化 → 调用 `ServicePromptMerger.MergeGlobalPrompts`
4. **转换为 Prompt**: `GlobalPrompt.ToPrompt()` 创建带标记的 `Prompt` 实例
5. **保存时过滤**: `PluginContext.SaveSettingStorage()` 过滤全局提示词，避免重复保存

## 核心类说明

### 1. GlobalPrompt

**位置**: `src/STranslate/Models/GlobalPrompt.cs`

全局提示词的数据模型，包含唯一标识、名称和提示词内容列表。

```csharp
public partial class GlobalPrompt : ObservableObject
{
    public string Id { get; set; }              // GUID 格式唯一标识
    public string Name { get; set; }            // 显示名称
    public ObservableCollection<PromptItem> Items { get; set; }  // 提示词内容
    
    public Prompt ToPrompt(bool isEnabled = false)  // 转换为 Prompt（注入时使用）
    public GlobalPrompt Clone()                      // 克隆（创建副本）
    public static GlobalPrompt CreateDefault(...)    // 创建默认提示词
}
```

**关键特性**:
- ID 使用 `Guid.NewGuid().ToString("N")` 生成（无连字符）
- `ToPrompt()` 生成带标记的 Prompt：名称格式为 `[Global:{Id}] {Name}`，Tag 设置为 `"Global:{Id}"`
- 双重 ID 保障：同时存储在名称和 Tag 中

### 2. ServicePromptMerger

**位置**: `src/STranslate/Core/ServicePromptMerger.cs`

负责将全局提示词合并到 AI 翻译服务的静态工具类。

```csharp
public static class ServicePromptMerger
{
    // 线程安全操作
    public static void MergeGlobalPrompts(Service service, Settings globalSettings)
    public static void ReMergeGlobalPrompts(Service service, Settings globalSettings)
    
    // 识别与提取
    public static bool IsGlobalPrompt(Prompt prompt)
    public static string? ExtractGlobalId(Prompt prompt)
    public static string GetDisplayName(Prompt prompt)
    
    // 保存时过滤
    public static bool TryFilterGlobalPrompts<T>(T settings, out List<Prompt> filteredPrompts)
}
```

**线程安全**:
- 使用 `ReaderWriterLockSlim` 保证并发安全
- 所有写操作在 UI 线程执行（`Dispatcher.Invoke`）
- 使用 `Dictionary<Type, PropertyInfo?>` 缓存反射结果

**识别优先级**:
1. **第一优先级**: Tag 属性（`Global:{Id}`）
2. **第二优先级**: 名称前缀（`[Global:{Id}]`）
3. **第三优先级**: 旧格式（`[Global]`，无法精确识别 ID）

### 3. GlobalPromptManager

**位置**: `src/STranslate/Core/GlobalPromptManager.cs`

全局提示词生命周期管理器，负责事件监听和同步。

```csharp
public class GlobalPromptManager : IDisposable
{
    public GlobalPromptManager(Settings settings, ServiceManager serviceManager)
    
    // 初始化
    public void InitializeAllServices()
    public void RegisterService(Service service)
    
    // 同步操作
    public void ReMergeAllAiServices()
    public void SaveGlobalPrompts()
    
    // IDisposable
    public void Dispose()
}
```

**事件监听**:
- `Settings.GlobalPrompts.CollectionChanged`: 全局提示词变更时重新合并
- `ServiceManager.ServiceAdded`: 新服务添加时自动合并
- `TranslationOptions.ReferencedGlobalPromptIds.CollectionChanged`: 引用变更时重新合并

### 4. GlobalPromptEditWindow

**位置**: `src/STranslate/Views/GlobalPromptEditWindow.xaml`

全局提示词编辑窗口，提供完整的 CRUD 操作界面。

**特性**:
- 与局部提示词编辑窗口布局一致
- 支持主题色跟随（亮色/暗色模式）
- 左侧列表支持拖拽排序
- 右侧编辑区支持角色切换和内容编辑
- 底部保存/取消按钮

### 5. GlobalPromptViewModel

**位置**: `src/STranslate/ViewModels/Pages/GlobalPromptViewModel.cs`

编辑窗口的 ViewModel，处理所有业务逻辑。

```csharp
public partial class GlobalPromptViewModel : ObservableObject
{
    public ObservableCollection<SelectableGlobalPrompt> GlobalPrompts { get; }
    public SelectableGlobalPrompt? SelectedPrompt { get; set; }
    public PromptItem? SelectedPromptItem { get; set; }
    
    // 命令
    public RelayCommand AddGlobalPromptCommand { get; }
    public RelayCommand DeleteGlobalPromptCommand { get; }
    public RelayCommand CloneGlobalPromptCommand { get; }
    public RelayCommand AddPromptItemCommand { get; }
    public RelayCommand RemovePromptItemCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand CancelCommand { get; }
}
```

### 6. SelectableGlobalPrompt

**位置**: `src/STranslate/ViewModels/SelectableGlobalPrompt.cs`

UI 绑定用的包装类，为列表项提供额外属性。

```csharp
public partial class SelectableGlobalPrompt : ObservableObject
{
    public GlobalPrompt GlobalPrompt { get; }      // 原始数据
    public bool IsSelected { get; set; }           // 是否选中
    public bool IsEnabled { get; set; }            // 是否启用（开关绑定）
    public int ReferenceCount { get; set; }        // 引用计数
    public string Id => GlobalPrompt.Id;
    public string Name => GlobalPrompt.Name;
}
```

### 7. LlmTranslatePluginBase 增强

**位置**: `src/STranslate.Plugin/ITranslatePlugin.cs`

大语言模型翻译插件基类，添加提示词快照机制。

```csharp
public abstract class LlmTranslatePluginBase : TranslatePluginBase, ILlm
{
    public ObservableCollection<Prompt> Prompts { get; set; } = []
    
    // 快照机制（线程安全）
    public IReadOnlyList<Prompt> GetPromptsSnapshot()   // 读取快照
    public void UpdateSnapshot()                         // 更新快照
    public virtual bool IsGlobalPrompt(Prompt prompt)   // 判断是否为全局提示词
}
```

### 8. TranslationOptions 扩展

**位置**: `src/STranslate.Plugin/Service.cs`

```csharp
public partial class TranslationOptions : ObservableObject
{
    // 新增属性
    public ObservableCollection<string> ReferencedGlobalPromptIds { get; set; } = []
    public Dictionary<string, bool> GlobalPromptStates { get; set; } = []
}
```

### 9. Settings 扩展

**位置**: `src/STranslate/Core/Settings.cs`

```csharp
public partial class Settings : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<GlobalPrompt> GlobalPrompts { get; set; } = []
}
```

### 10. Prompt 扩展

**位置**: `src/STranslate.Plugin/Prompt.cs`

```csharp
public partial class Prompt : ObservableObject
{
    [JsonIgnore]
    public object? Tag { get; set; }   // 存储全局提示词 ID（不序列化）
}
```

## 使用方式

### 用户操作流程

1. **打开编辑窗口**
   - 进入 设置 → 服务 → 文本翻译
   - 点击右上角 "全局提示词：[编辑]" 按钮

2. **创建全局提示词**
   - 点击 "添加" 按钮
   - 输入提示词名称
   - 添加角色项（system/user/assistant）
   - 编辑每个角色的内容

3. **使用变量**
   - `$source` - 源语言
   - `$target` - 目标语言
   - `$content` - 待翻译内容

4. **保存更改**
   - 点击 "保存" 按钮保存并关闭
   - 或直接关闭窗口（自动保存）

### 开发者 API

```csharp
// 创建全局提示词
var globalPrompt = new GlobalPrompt
{
    Name = "专业翻译",
    Items =
    [
        new PromptItem("system", "你是一个专业的翻译助手"),
        new PromptItem("user", "请将以下$content从$source翻译为$target")
    ]
};
settings.GlobalPrompts.Add(globalPrompt);

// 在服务中引用
service.Options.ReferencedGlobalPromptIds.Add(globalPrompt.Id);
service.Options.GlobalPromptStates[globalPrompt.Id] = true;

// 判断是否为全局提示词
if (ServicePromptMerger.IsGlobalPrompt(prompt))
{
    var id = ServicePromptMerger.ExtractGlobalId(prompt);
    var displayName = ServicePromptMerger.GetDisplayName(prompt);
}
```

## 文件清单

### 新增文件

| 文件路径 | 说明 |
|---------|------|
| `src/STranslate/Models/GlobalPrompt.cs` | 全局提示词数据模型 |
| `src/STranslate/Core/ServicePromptMerger.cs` | 提示词合并逻辑 |
| `src/STranslate/Core/GlobalPromptManager.cs` | 全局提示词管理器 |
| `src/STranslate/Views/GlobalPromptEditWindow.xaml` | 编辑窗口 UI |
| `src/STranslate/Views/GlobalPromptEditWindow.xaml.cs` | 编辑窗口代码 |
| `src/STranslate/ViewModels/Pages/GlobalPromptViewModel.cs` | 编辑窗口 ViewModel |
| `src/STranslate/ViewModels/SelectableGlobalPrompt.cs` | UI 绑定包装类 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|----------|
| `src/STranslate.Plugin/Prompt.cs` | 添加 Tag 属性 |
| `src/STranslate.Plugin/ITranslatePlugin.cs` | LlmTranslatePluginBase 添加快照机制 |
| `src/STranslate.Plugin/Service.cs` | TranslationOptions 扩展 |
| `src/STranslate/Core/Settings.cs` | 添加 GlobalPrompts 属性 |
| `src/STranslate/Core/ServiceManager.cs` | 添加 ServiceAdded/ServiceRemoved 事件 |
| `src/STranslate/Core/PluginContext.cs` | 保存时过滤全局提示词 |
| `src/STranslate/App.xaml.cs` | 注册 GlobalPromptManager 并初始化 |
| `src/STranslate/Views/Pages/TranslatePage.xaml` | 添加编辑按钮 |
| `src/STranslate/ViewModels/Pages/TranslateViewModel.cs` | 添加 EditGlobalPrompts 命令 |
| `src/STranslate/Views/SettingsWindow.xaml` | 调整窗口宽度 |
| `src/STranslate/Languages/zh-cn.xaml` | 添加相关语言资源 |
| `src/STranslate/Languages/en.xaml` | 添加相关语言资源 |

## 常见问题

### Q: 全局提示词编辑窗口在哪里？

A: 打开设置窗口 → 服务 → 文本翻译 → 右上角工具栏 → 点击 "全局提示词：[编辑]" 按钮。

### Q: 为什么全局提示词没有显示在服务中？

A: 请检查服务的 `ReferencedGlobalPromptIds` 是否包含该提示词的 ID。

### Q: 保存插件设置时为什么全局提示词消失了？

A: 这是正常现象。全局提示词在保存时会被过滤掉，由全局设置统一管理。

### Q: 编辑窗口的主题为什么不跟随软件主题？

A: 已修复。编辑窗口使用 `ThemeManager.SetRequestedTheme()` 自动跟随主程序主题。

## 版本历史

- **v1.0** (2026-02-18): 初始版本实现
  - 支持全局提示词创建、编辑、删除
  - 独立编辑窗口，支持主题跟随
  - 完整的线程安全保证
  - 向后兼容设计
