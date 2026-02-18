# 全局提示词功能文档

## 概述

全局提示词（Global Prompts）是 STranslate 的一项高级功能，允许用户创建可在多个 AI 翻译服务间共享的提示词模板。这解决了重复配置相同提示词的问题，提供了统一的提示词管理体验。

## 功能特性

- **集中管理**: 在一个地方创建和维护提示词，多处使用
- **自动同步**: 修改全局提示词后，所有引用的服务自动更新
- **状态保持**: 每个服务可以独立控制全局提示词的启用/禁用状态
- **线程安全**: 完整的并发控制，支持多线程环境下的安全操作
- **向后兼容**: 不影响现有插件和服务的正常运行

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

1. **创建全局提示词**: 用户创建 `GlobalPrompt` → 添加到 `Settings.GlobalPrompts`
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

**初始化流程**:
1. 订阅全局提示词集合变化事件
2. 订阅服务添加事件
3. 遍历所有已加载的 AI 服务，执行初始合并
4. 为每个服务注册引用变更监听

### 4. LlmTranslatePluginBase 增强

**位置**: `src/STranslate.Plugin/ITranslatePlugin.cs`

大语言模型翻译插件基类，添加提示词快照机制。

```csharp
public abstract class LlmTranslatePluginBase : TranslatePluginBase, ILlm
{
    public ObservableCollection<Prompt> Prompts { get; set; } = []
    
    // 快照机制（线程安全）
    private IReadOnlyList<Prompt> _promptsSnapshot = []
    private readonly ReaderWriterLockSlim _snapshotLock = new()
    
    public IReadOnlyList<Prompt> GetPromptsSnapshot()   // 读取快照（翻译时使用）
    public void UpdateSnapshot()                         // 更新快照
    public DateTime LastSnapshotUpdate { get; }          // 上次更新时间
    
    public virtual bool IsGlobalPrompt(Prompt prompt)    // 判断是否为全局提示词
}
```

**快照机制**:
- 使用 `ReaderWriterLockSlim` 保证读写安全
- `Prompts.CollectionChanged` 事件触发时自动更新快照
- 翻译时使用 `GetPromptsSnapshot()` 获取只读快照，避免并发修改问题

**线程安全保证**:
- 读取快照：获取读锁 → 返回快照 → 释放读锁
- 更新快照：获取写锁 → 创建新列表 → 更新引用 → 释放写锁

### 5. TranslationOptions 扩展

**位置**: `src/STranslate.Plugin/Service.cs`

翻译服务选项，添加全局提示词相关配置。

```csharp
public partial class TranslationOptions : ObservableObject
{
    public ExecutionMode ExecMode { get; set; }
    public bool AutoBackTranslation { get; set; }
    
    // 新增属性
    public ObservableCollection<string> ReferencedGlobalPromptIds { get; set; } = []
    public Dictionary<string, bool> GlobalPromptStates { get; set; } = []
}
```

**属性说明**:
- `ReferencedGlobalPromptIds`: 服务引用的全局提示词 ID 列表
- `GlobalPromptStates`: 全局提示词启用状态字典（Key: ID, Value: 是否启用）

### 6. Settings 扩展

**位置**: `src/STranslate/Core/Settings.cs`

应用设置，添加全局提示词集合。

```csharp
public partial class Settings : ObservableObject
{
    // 现有属性...
    
    // 新增属性
    [ObservableProperty]
    public partial ObservableCollection<GlobalPrompt> GlobalPrompts { get; set; } = []
}
```

### 7. Prompt 扩展

**位置**: `src/STranslate.Plugin/Prompt.cs`

提示词模型，添加 Tag 属性用于存储元数据。

```csharp
public partial class Prompt : ObservableObject
{
    public string Name { get; set; }
    public ObservableCollection<PromptItem> Items { get; set; } = []
    public bool IsEnabled { get; set; }
    
    // 新增属性
    [JsonIgnore]
    public object? Tag { get; set; }   // 存储全局提示词 ID（不序列化）
}
```

### 8. PluginContext 增强

**位置**: `src/STranslate/Core/PluginContext.cs`

插件上下文，添加保存时过滤全局提示词功能。

```csharp
public class PluginContext : IPluginContext
{
    private object? _currentSettings;
    
    public void SaveSettingStorage<T>() where T : new()
    {
        // 1. 尝试过滤全局提示词
        // 2. 保存设置
        // 3. 恢复原始提示词列表（内存中）
    }
}
```

**过滤逻辑**:
1. 调用 `ServicePromptMerger.TryFilterGlobalPrompts()` 获取过滤后的列表
2. 临时替换 `Settings.Prompts` 为过滤后的列表
3. 执行保存操作
4. 恢复原始提示词列表（保证 UI 正常显示全局提示词）

## 使用方式

### 创建全局提示词

```csharp
var globalPrompt = new GlobalPrompt
{
    Name = "专业翻译",
    Items =
    [
        new PromptItem("system", "你是一个专业的翻译助手"),
        new PromptItem("user", "请翻译以下内容：")
    ]
};

settings.GlobalPrompts.Add(globalPrompt);
```

### 在服务中引用

```csharp
// 在服务配置中添加引用
service.Options.ReferencedGlobalPromptIds.Add(globalPrompt.Id);

// 设置默认启用状态
service.Options.GlobalPromptStates[globalPrompt.Id] = true;
```

### 插件中检测全局提示词

```csharp
public void MyPluginMethod(LlmTranslatePluginBase plugin)
{
    foreach (var prompt in plugin.Prompts)
    {
        if (plugin.IsGlobalPrompt(prompt))
        {
            // 这是全局提示词
            var displayName = ServicePromptMerger.GetDisplayName(prompt);
            Console.WriteLine($"全局提示词: {displayName}");
        }
    }
}
```

### 翻译时使用快照

```csharp
public async Task<TranslateResult> TranslateAsync(...)
{
    // 获取提示词快照（线程安全）
    var promptsSnapshot = GetPromptsSnapshot();
    
    // 使用快照进行翻译，避免并发修改问题
    var enabledPrompt = promptsSnapshot.FirstOrDefault(p => p.IsEnabled);
    // ...
}
```

## 线程安全

### 并发场景处理

1. **全局提示词集合修改**
   - 由 `GlobalPromptManager` 监听 `CollectionChanged` 事件
   - 自动触发 `ReMergeAllAiServices()`
   - 使用 `Dispatcher.Invoke` 确保在 UI 线程执行

2. **服务引用变更**
   - 每个服务的 `ReferencedGlobalPromptIds.CollectionChanged` 事件
   - 调用 `ServicePromptMerger.ReMergeGlobalPrompts()`
   - 使用 `ReaderWriterLockSlim` 加写锁

3. **翻译时的并发读取**
   - 使用 `GetPromptsSnapshot()` 获取只读快照
   - `ReaderWriterLockSlim` 读锁保证读取安全
   - 快照在读取完成后即可释放锁，不阻塞写入

### 锁的使用策略

```csharp
// ServicePromptMerger 中的锁使用
private static readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);

// 写操作（合并、重新合并）
_lock.EnterWriteLock();
try
{
    // 修改 Prompts 集合
}
finally
{
    _lock.ExitWriteLock();
}

// LlmTranslatePluginBase 中的快照锁
private readonly ReaderWriterLockSlim _snapshotLock = new();

// 读快照
_snapshotLock.EnterReadLock();
try
{
    return _promptsSnapshot;
}
finally
{
    _snapshotLock.ExitReadLock();
}

// 写快照
_snapshotLock.EnterWriteLock();
try
{
    _promptsSnapshot = Prompts.ToList();
}
finally
{
    _snapshotLock.ExitWriteLock();
}
```

## 向后兼容

### 数据兼容

1. **旧版本设置文件**
   - `GlobalPrompts` 属性会被旧版本忽略
   - 不影响旧版本运行

2. **插件保存的设置**
   - 保存时自动过滤全局提示词
   - 旧版本读取时不会看到 `[Global:ID]` 格式的提示词

3. **运行时兼容**
   - 新添加的事件（`ServiceAdded`/`ServiceRemoved`）可选订阅
   - 不影响不使用全局提示词功能的代码

### 迁移策略

1. **平滑升级**: 用户升级到新版本后，原有配置保持不变
2. **功能启用**: 用户需要手动创建全局提示词并引用到服务
3. **降级支持**: 如需要回滚到旧版本，全局提示词数据会被保留在设置文件中

## 文件清单

### 新增文件

| 文件路径 | 说明 |
|---------|------|
| `src/STranslate/Models/GlobalPrompt.cs` | 全局提示词模型 |
| `src/STranslate/Core/ServicePromptMerger.cs` | 提示词合并逻辑 |
| `src/STranslate/Core/GlobalPromptManager.cs` | 全局提示词管理器 |

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

## 性能考虑

1. **反射缓存**: `ServicePromptMerger` 缓存 `PropertyInfo`，避免重复反射
2. **快照机制**: 避免翻译时加锁，提高并发性能
3. **延迟加载**: 全局提示词按需合并，不使用时无开销
4. **批量操作**: `ReMergeAllAiServices` 批量处理，减少重复计算

## 调试与日志

### 日志级别

- **Debug**: 过滤操作跳过时的提示
- **Error**: 过滤或合并失败时的错误信息
- **Warning**: 保存时过滤失败的警告

### 调试技巧

```csharp
// 检查全局提示词识别
var prompt = new Prompt { Tag = "Global:abc123", Name = "[Global:abc123] Test" };
var isGlobal = ServicePromptMerger.IsGlobalPrompt(prompt);  // true
var id = ServicePromptMerger.ExtractGlobalId(prompt);       // "abc123"
var displayName = ServicePromptMerger.GetDisplayName(prompt); // "Test"

// 检查快照状态
var llmPlugin = service.Plugin as LlmTranslatePluginBase;
var snapshot = llmPlugin.GetPromptsSnapshot();
var lastUpdate = llmPlugin.LastSnapshotUpdate;
```

## 常见问题

### Q: 为什么全局提示词没有显示在服务中？

A: 请检查：
1. 全局提示词是否已添加到 `Settings.GlobalPrompts`
2. 服务的 `TranslationOptions.ReferencedGlobalPromptIds` 是否包含该提示词的 ID
3. `GlobalPromptManager` 是否已初始化（检查 App.xaml.cs）

### Q: 修改全局提示词后为什么没有自动更新？

A: 请检查：
1. `GlobalPromptManager` 是否正确订阅了 `CollectionChanged` 事件
2. 修改是否在 UI 线程执行
3. 查看日志是否有错误信息

### Q: 保存插件设置时为什么全局提示词消失了？

A: 这是正常现象。全局提示词在保存时会被过滤掉，因为它们由全局设置统一管理。下次加载时会自动重新合并。

### Q: 如何实现线程安全的翻译？

A: 使用 `GetPromptsSnapshot()` 方法获取提示词快照：

```csharp
var prompts = GetPromptsSnapshot();
var enabledPrompt = prompts.FirstOrDefault(p => p.IsEnabled);
// 使用 enabledPrompt 进行翻译，不受其他线程修改影响
```

## 版本历史

- **v1.0** (2026-02-18): 初始版本实现
  - 支持全局提示词创建、引用、同步
  - 完整的线程安全保证
  - 向后兼容设计

## 参考文档

- [全局提示词功能实现方案](../全局提示词功能实现方案/全局提示词功能实现方案_最终实施版.md)
- [插件开发指南](plugin.md)
- [架构设计](architecture.md)
