# 全局提示词功能文档（统一Prompt类方案）

## 概述

全局提示词（Global Prompts）是 STranslate 的一项高级功能，允许用户在主程序中创建可共享的提示词模板。本实现采用**统一Prompt类**方案，将全局提示词视为主程序的"局部提示词"，通过 `IPluginContext` 接口暴露给插件。

## 设计原则

- **概念统一**: 全局和局部提示词都使用 `Prompt` 类，只是存储位置不同
- **明确分离**: 全局提示词存储在主程序，局部提示词存储在插件配置
- **插件自主**: 插件决定是否使用、如何使用全局提示词
- **接口暴露**: 通过 `IPluginContext` 接口获取全局提示词
- **实时通知**: 支持事件回调，当全局提示词变更时主动通知插件

## 核心架构

### 设计理念

```
主程序Settings.json          插件配置
├─ GlobalPrompts[]   ────→   通过接口读取
│  (Prompt类型)               (Prompt类型)
│  IsEnabled = 是否暴露
│
插件内部
├─ Prompts[]         ←────   局部提示词
   (Prompt类型)              互斥选择（翻译时）
```

### 与局部提示词的区别

| 特性 | 全局提示词 | 局部提示词 |
|------|-----------|-----------|
| **存储位置** | 主程序 Settings.json | 插件配置文件 |
| **IsEnabled语义** | 是否暴露给插件（可多选） | 翻译时是否启用（互斥） |
| **作用域** | 所有插件共享 | 仅当前插件 |
| **管理方式** | 主程序管理，接口暴露 | 插件自己管理 |

## 核心接口

### IPluginContext 扩展

```csharp
public interface IPluginContext : IDisposable
{
    // 现有方法...

    /// <summary>
    /// 获取所有已启用的全局提示词列表（只读）
    /// 注意：只返回 IsEnabled = true 的全局提示词
    /// </summary>
    IReadOnlyList<Prompt> GetGlobalPrompts();

    /// <summary>
    /// 注册全局提示词变更回调。
    /// 当全局提示词发生添加、删除、修改或启用状态变化时，主软件会调用此回调通知插件。
    /// </summary>
    /// <param name="callback">回调函数，参数为变更后的全局提示词只读列表</param>
    /// <param name="lifetime">可选的生命周期管理器（如窗口对象），当管理器释放时自动注销回调</param>
    /// <returns>用于手动注销回调的句柄</returns>
    IDisposable RegisterGlobalPromptsChangedCallback(Action<IReadOnlyList<Prompt>> callback, IDisposable? lifetime = null);

    /// <summary>
    /// 注销全局提示词变更回调。
    /// 插件卸载时应调用此方法以避免内存泄漏。
    /// 注意：如果注册时提供了 lifetime 参数，回调会在 lifetime 释放时自动注销。
    /// </summary>
    /// <param name="callback">之前注册的回调函数</param>
    void UnregisterGlobalPromptsChangedCallback(Action<IReadOnlyList<Prompt>> callback);
}
```

### Prompt 类

全局提示词和局部提示词都使用相同的 `Prompt` 类：

```csharp
public partial class Prompt : ObservableObject
{
    public string Name { get; set; }              // 显示名称
    public bool IsEnabled { get; set; }          // 对于全局提示词：是否暴露给插件
    public ObservableCollection<PromptItem> Items { get; set; }  // 内容列表
    public object? Tag { get; set; }             // 标签（用于存储元数据，不序列化）
    
    // 克隆方法
    public Prompt Clone();
}

public partial class PromptItem : ObservableObject
{
    public string Role { get; set; }      // 角色：system/user/assistant
    public string Content { get; set; }   // 内容
    
    // 克隆方法
    public PromptItem Clone();
}
```

## 启用/禁用机制

### 开关作用

全局提示词编辑窗口中的**拨动开关**控制该提示词是否暴露给插件：

| 开关状态 | 效果 |
|---------|------|
| **开启** (默认) | 插件可通过 `GetGlobalPrompts()` 获取到该提示词 |
| **关闭** | 插件**无法**获取到该提示词，接口会过滤掉 |

### 实现原理

```csharp
// PluginContext.cs
public IReadOnlyList<Prompt> GetGlobalPrompts()
{
    return Settings.GlobalPrompts
        .Where(p => p.IsEnabled)  // 只返回启用的
        .Select(p => p.Clone())   // 返回副本保护原始数据
        .ToList()
        .AsReadOnly();
}
```

### 与局部提示词的区别

| 特性 | 全局提示词开关 | 局部提示词开关 |
|------|---------------|---------------|
| **作用** | 控制是否暴露给插件 | 控制是否在翻译时使用 |
| **UI位置** | 全局提示词编辑窗口 | 插件设置窗口 |
| **互斥性** | 非互斥（可多选） | 互斥（只能选一个） |
| **影响范围** | 所有使用该提示词的插件 | 仅当前插件 |

## 插件使用方式

### 1. 获取已启用的全局提示词列表

```csharp
public class MyLlmPlugin : LlmTranslatePluginBase
{
    public override void Init(IPluginContext context)
    {
        // 获取所有已启用的全局提示词（直接是Prompt类型）
        var globalPrompts = context.GetGlobalPrompts();
        
        foreach (var prompt in globalPrompts)
        {
            Console.WriteLine($"全局提示词: {prompt.Name}");
            
            // 直接使用，无需转换
            if (ShouldUsePrompt(prompt.Name))
            {
                Prompts.Add(prompt);
            }
        }
    }
}
```

### 2. 处理提示词角色

```csharp
public void ProcessPrompt(Prompt prompt)
{
    // 遍历 Items，每个都有独立的角色
    foreach (var item in prompt.Items)
    {
        // item.Role 可能是: "system", "user", "assistant"
        // item.Content 是对应的内容
        Console.WriteLine($"角色: {item.Role}, 内容: {item.Content}");
    }
    
    // 按角色分别处理
    var systemMessage = prompt.Items.FirstOrDefault(i => i.Role == "system");
    var userMessage = prompt.Items.FirstOrDefault(i => i.Role == "user");
    
    if (systemMessage != null)
    {
        // system 消息通常用于定义 AI 的行为
        // 例如: "你是一个专业的翻译助手"
    }
    
    if (userMessage != null)
    {
        // user 消息通常是具体的指令
        // 例如: "请翻译以下内容："
    }
}
```

**关键点：**
- ✅ 每个角色独立存储，不会被合并或丢失
- ✅ 插件可以按角色分别处理或组合使用
- ✅ 直接将 `prompt.Items` 传递给 AI API 时，角色信息会正确保留

### 3. 监听全局提示词变更（推荐）

```csharp
public class MyLlmPlugin : LlmTranslatePluginBase
{
    public override void Init(IPluginContext context)
    {
        // 注册变更回调，实时获取更新
        context.RegisterGlobalPromptsChangedCallback(OnGlobalPromptsChanged);
        
        // 初始加载
        RefreshGlobalPrompts(context.GetGlobalPrompts());
    }

    private void OnGlobalPromptsChanged(IReadOnlyList<Prompt> globalPrompts)
    {
        // 收到通知后立即刷新
        RefreshGlobalPrompts(globalPrompts);
    }

    private void RefreshGlobalPrompts(IReadOnlyList<Prompt> globalPrompts)
    {
        // 更新插件内部状态
        // 注意：可能需要根据名称匹配来更新已有提示词
    }

    public override void Dispose()
    {
        // 注销回调，避免内存泄漏
        Context.UnregisterGlobalPromptsChangedCallback(OnGlobalPromptsChanged);
        base.Dispose();
    }
}
```

### 4. 窗口生命周期管理（推荐）

**适用场景**：插件只在特定窗口打开时需要监听全局提示词变更。

```csharp
public partial class MyPluginSettingsWindow : Window
{
    private IDisposable? _callbackHandle;

    public MyPluginSettingsWindow()
    {
        InitializeComponent();
        
        // 窗口加载时注册回调，传入窗口作为生命周期管理器
        // 当窗口关闭时，回调会自动注销，无需手动处理
        _callbackHandle = Context.RegisterGlobalPromptsChangedCallback(
            OnGlobalPromptsChanged, 
            lifetime: this  // 传入窗口对象
        );
    }

    private void OnGlobalPromptsChanged(IReadOnlyList<Prompt> globalPrompts)
    {
        // 刷新界面显示
        RefreshUI(globalPrompts);
    }

    // 也可以手动注销（如果需要提前释放）
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _callbackHandle?.Dispose(); // 立即注销
        this.Close();
    }
}
```

**优势**：
- ✅ 自动管理回调生命周期，窗口关闭即释放
- ✅ 避免常驻托盘软件的内存泄漏问题
- ✅ 代码更简洁，无需在 Dispose 中处理
- ✅ 向后兼容，不传 `lifetime` 参数时行为不变

### 5. 保存引用

```csharp
public void SaveSettings()
{
    // 保存引用的全局提示词名称列表
    Settings.ReferencedGlobalPromptNames = Prompts
        .Where(p => IsFromGlobal(p))
        .Select(p => p.Name)
        .ToList();
    
    Context.SaveSettingStorage<MySettings>();
}

// 判断是否为全局提示词（可根据Tag或其他机制）
private bool IsFromGlobal(Prompt prompt)
{
    // 可以根据具体业务逻辑判断
    // 例如：Tag标记、名称前缀等
    return prompt.Tag?.ToString()?.StartsWith("Global:") == true;
}
```

## 事件回调机制

### 工作原理

当用户在主软件中修改全局提示词（添加、删除、编辑或更改启用状态）时，主软件会：

1. 触发 `Settings.GlobalPromptsChanged` 事件
2. `PluginContext` 接收事件并调用所有已注册的回调
3. 插件收到回调后可立即更新内部状态

### 线程安全

- 回调可能在任意线程上执行
- 如果需要更新 UI，请使用调度器：

```csharp
private void OnGlobalPromptsChanged(IReadOnlyList<Prompt> globalPrompts)
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        // 安全地更新 UI
        RefreshGlobalPromptsUI(globalPrompts);
    });
}
```

### 多次注册保护

接口内部会自动忽略重复注册：

```csharp
// 这只会注册一次
Context.RegisterGlobalPromptsChangedCallback(MyCallback);
Context.RegisterGlobalPromptsChangedCallback(MyCallback); // 被忽略
```

## 用户界面

### 入口位置

**设置窗口 → 服务 → 文本翻译** 页面右上角：

```
全局提示词：[编辑] | 图片翻译：... | 替换翻译：...
```

### 编辑窗口

独立的编辑窗口，支持：
- 添加/复制/删除全局提示词
- 编辑提示词内容（角色和文本）
- **启用/禁用开关** - 控制是否暴露给插件
- 变量说明（$source, $target, $content）

## 架构对比

### 新旧方案对比

| 特性 | 旧方案（GlobalPrompt类） | 新方案（统一Prompt类） |
|------|------------------------|---------------------|
| **数据结构** | GlobalPrompt + Prompt 两个类 | 只有 Prompt 一个类 |
| **类型转换** | 需要 ToPrompt() 转换 | 无需转换，直接使用 |
| **唯一标识** | 使用 Id 字段 | 使用 Name 字段 |
| **概念复杂度** | 中（全局/局部区分） | 低（都是 Prompt） |
| **代码量** | 较多（100+行 GlobalPrompt） | 较少（删除 GlobalPrompt） |
| **插件影响** | 需要适配 GlobalPrompt 类型 | 直接使用 Prompt，更简单 |
| **灵活性** | 中 | 高 |

## 文件结构

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `IPluginContext.cs` | 接口返回类型从 `GlobalPrompt` 改为 `Prompt`，删除 `GetGlobalPromptById` |
| `PluginContext.cs` | 实现接口变更，类型改为 `Prompt` |
| `Settings.cs` | `GlobalPrompts` 类型改为 `ObservableCollection<Prompt>` |
| `GlobalPromptViewModel.cs` | 使用 `Prompt` 替代 `GlobalPrompt` |
| `GlobalPromptEditWindow.xaml` | 绑定调整 |

### 删除文件

| 文件 | 原因 |
|------|------|
| `GlobalPrompt.cs` | 统一使用 Prompt 类，不再需要 GlobalPrompt |

## 迁移指南

### 对于已使用全局提示词的插件

**旧代码（GlobalPrompt方案）：**
```csharp
var globalPrompts = Context.GetGlobalPrompts();  // 返回 GlobalPrompt
foreach (var gp in globalPrompts)
{
    // 需要转换
    var prompt = gp.ToPrompt(isEnabled: true);
    Prompts.Add(prompt);
}

// 通过ID获取
var specific = Context.GetGlobalPromptById("abc123");
```

**新代码（统一Prompt方案）：**
```csharp
var globalPrompts = Context.GetGlobalPrompts();  // 直接返回 Prompt
foreach (var prompt in globalPrompts)
{
    // 直接使用
    Prompts.Add(prompt);
}

// 通过名称获取（或遍历查找）
var specific = globalPrompts.FirstOrDefault(p => p.Name == "xxx");
```

**主要改动：**
1. 删除 `.ToPrompt()` 调用
2. 删除 `GetGlobalPromptById()` 使用
3. 直接使用 `Prompt` 对象

## 常见问题

### Q: 关闭开关后，插件还能获取到该提示词吗？

A: **不能**。关闭开关后，`GetGlobalPrompts()` 不会返回该提示词。

### Q: 修改全局提示词后，插件会自动更新吗？

A: **会**。如果插件注册了 `RegisterGlobalPromptsChangedCallback` 回调，主软件会在全局提示词变更时主动通知插件。

### Q: 如果插件不注册回调会怎样？

A: 插件需要在下次调用 `GetGlobalPrompts()` 时获取最新数据，无法实时感知变更。

### Q: 插件可以修改全局提示词吗？

A: 不应该修改。全局提示词由主程序统一管理，插件只应该读取和使用。

### Q: 全局提示词的开关和局部提示词的开关有什么区别？

A: 
- **全局提示词开关**: 控制是否**暴露给插件**（能否被获取，可多选）
- **局部提示词开关**: 控制是否在**翻译时启用**（是否被使用，互斥）

### Q: 回调中的异常会影响其他插件吗？

A: **不会**。每个回调的异常会被单独捕获并记录日志，不会影响其他回调的执行。

### Q: 如果两个全局提示词同名，插件如何区分？

A: 插件可以通过遍历获取所有同名提示词，自行决定如何处理。推荐在编辑窗口中避免重名。

### Q: 全局提示词改名后，插件如何更新引用？

A: 插件收到 `RegisterGlobalPromptsChangedCallback` 回调后，需要根据新的名称列表重新匹配和更新内部状态。

## 版本历史

- **v3.0** (2026-02-19): 统一使用 Prompt 类重构
  - 删除 GlobalPrompt 类
  - 全局提示词和局部提示词统一使用 Prompt 类
  - 简化架构，减少概念复杂度
  - 删除 ToPrompt() 转换方法
  - 删除 GetGlobalPromptById() 接口方法
  - 插件可以直接使用 Prompt 对象，无需转换

- **v2.2** (2026-02-19): 优化角色处理和内存管理
  - 修改 `ToPrompt()` 方法，显式创建 PromptItem 确保 Role 正确传递
  - 保持原始名称，不再添加 `[Global:{Id}]` 前缀
  - 添加窗口生命周期管理，避免常驻托盘软件的内存泄漏
  - `RegisterGlobalPromptsChangedCallback` 返回 IDisposable，支持自动注销
  - 新增 `lifetime` 参数，窗口关闭时自动注销回调

- **v2.1** (2026-02-18): 添加事件回调机制
  - 新增 RegisterGlobalPromptsChangedCallback/UnregisterGlobalPromptsChangedCallback 接口
  - 支持实时通知插件全局提示词变更
  - 线程安全的回调管理

- **v2.0** (2026-02-18): 接口暴露方式实现
  - 通过 IPluginContext 接口获取全局提示词
  - 明确分离全局和局部提示词
  - 移除自动合并和引用追踪机制
  - IsEnabled 开关控制是否暴露给插件
