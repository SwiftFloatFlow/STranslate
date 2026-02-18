# 全局提示词功能文档（接口暴露方式）

## 概述

全局提示词（Global Prompts）是 STranslate 的一项高级功能，允许用户在主程序中创建可共享的提示词模板。与自动合并方式不同，本实现采用**接口暴露**方式，插件通过 `IPluginContext` 接口主动获取全局提示词，保持全局和局部提示词的明确分离。

## 设计原则

- **明确分离**: 全局提示词和局部提示词完全分开管理
- **插件自主**: 插件决定是否使用、如何使用全局提示词
- **接口暴露**: 通过 `IPluginContext` 接口获取全局提示词
- **无隐式行为**: 不自动合并、不自动同步

## 核心接口

### IPluginContext 扩展

```csharp
public interface IPluginContext : IDisposable
{
    // 现有方法...

    /// <summary>
    /// 获取所有全局提示词列表（只读）
    /// </summary>
    IReadOnlyList<GlobalPrompt> GetGlobalPrompts();

    /// <summary>
    /// 根据ID获取特定的全局提示词
    /// </summary>
    GlobalPrompt? GetGlobalPromptById(string id);
}
```

### GlobalPrompt 类

```csharp
public partial class GlobalPrompt : ObservableObject
{
    public string Id { get; set; }              // 唯一标识
    public string Name { get; set; }            // 显示名称
    public ObservableCollection<PromptItem> Items { get; set; }  // 内容列表
    
    // 转换为可用的 Prompt
    public Prompt ToPrompt(bool isEnabled = false);
    
    // 克隆
    public GlobalPrompt Clone();
}
```

## 插件使用方式

### 1. 获取全局提示词列表

```csharp
public class MyLlmPlugin : LlmTranslatePluginBase
{
    public override void Initialize()
    {
        // 获取所有全局提示词
        var globalPrompts = Context.GetGlobalPrompts();
        
        // 根据需要选择使用
        foreach (var globalPrompt in globalPrompts)
        {
            Console.WriteLine($"全局提示词: {globalPrompt.Name}");
        }
    }
}
```

### 2. 使用特定全局提示词

```csharp
public void UseGlobalPrompt(string promptId)
{
    // 通过ID获取特定提示词
    var globalPrompt = Context.GetGlobalPromptById(promptId);
    
    if (globalPrompt != null)
    {
        // 转换并添加到插件的 Prompts 集合
        var prompt = globalPrompt.ToPrompt(isEnabled: true);
        Prompts.Add(prompt);
    }
}
```

### 3. 合并显示（可选）

```csharp
// 插件可以选择在UI中合并显示
public ObservableCollection<Prompt> AllPrompts
{
    get
    {
        var all = new ObservableCollection<Prompt>(Prompts);
        
        // 添加全局提示词（标记为全局）
        foreach (var global in Context.GetGlobalPrompts())
        {
            var prompt = global.ToPrompt();
            prompt.Tag = "Global";  // 标记来源
            all.Add(prompt);
        }
        
        return all;
    }
}
```

### 4. 保存时区分

```csharp
public void SaveSettings()
{
    // 只保存局部提示词（全局的由主程序管理）
    var localPrompts = Prompts.Where(p => p.Tag?.ToString() != "Global").ToList();
    Settings.Prompts = new ObservableCollection<Prompt>(localPrompts);
    
    Context.SaveSettingStorage<MySettings>();
}
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
- 启用/禁用开关
- 变量说明（$source, $target, $content）

## 架构对比

### 方案一（自动合并）vs 方案二（接口暴露）

| 特性 | 方案一（自动合并） | 方案二（接口暴露） |
|------|-------------------|-------------------|
| 获取方式 | 自动混入 Prompts | 通过接口主动获取 |
| 分离性 | 隐式混合 | 明确分离 |
| 插件改动 | 无需改动 | 需要调用接口 |
| 状态追踪 | 复杂（引用计数） | 简单（无追踪） |
| 保存处理 | 需要过滤 | 各管各的 |
| 灵活性 | 低 | 高 |

## 文件结构

### 新增文件

| 文件 | 说明 |
|------|------|
| `STranslate.Plugin/GlobalPrompt.cs` | 全局提示词模型（在Plugin库中） |

### 修改文件

| 文件 | 修改内容 |
|------|----------|
| `IPluginContext.cs` | 添加 GetGlobalPrompts/GetGlobalPromptById |
| `PluginContext.cs` | 实现新接口方法 |
| `Settings.cs` | 添加 GlobalPrompts 集合 |

### 删除文件（相比方案一）

| 文件 | 原因 |
|------|------|
| `GlobalPromptManager.cs` | 不需要自动合并管理 |
| `ServicePromptMerger.cs` | 不需要合并逻辑 |

## 常见问题

### Q: 如何判断一个 Prompt 是否来自全局？

A: 通过 Tag 属性判断：
```csharp
if (prompt.Tag?.ToString()?.StartsWith("Global:") == true)
{
    // 这是全局提示词
}
```

### Q: 修改全局提示词后，插件会自动更新吗？

A: 不会自动更新。插件需要在下次调用 `GetGlobalPrompts()` 时获取最新数据。

### Q: 插件可以修改全局提示词吗？

A: 不应该修改。全局提示词由主程序统一管理，插件只应该读取和转换使用。

## 版本历史

- **v2.0** (2026-02-18): 接口暴露方式实现
  - 通过 IPluginContext 接口获取全局提示词
  - 明确分离全局和局部提示词
  - 移除自动合并和引用追踪机制
