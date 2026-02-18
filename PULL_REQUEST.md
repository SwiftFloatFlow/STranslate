# Pull Request 说明

## 标题

```
feat: 添加全局提示词功能（接口暴露方式）
```

---

## 功能描述

本PR实现了**全局提示词（Global Prompts）**功能，允许用户在主程序中创建可复用的提示词模板，并通过接口暴露给AI翻译插件使用。

---

## 功能特性

- ✅ 独立的全局提示词编辑窗口
- ✅ 支持启用/禁用控制（禁用后不暴露给插件）
- ✅ 通过 `IPluginContext` 接口暴露，插件主动获取
- ✅ 全局和局部提示词明确分离
- ✅ 支持变量替换（$source, $target, $content）
- ✅ 主题色跟随（亮色/暗色模式）

---

## 接口变更

### IPluginContext 新增方法

```csharp
/// <summary>
/// 获取所有已启用的全局提示词列表（只读）
/// </summary>
IReadOnlyList<GlobalPrompt> GetGlobalPrompts();

/// <summary>
/// 根据ID获取特定的全局提示词（仅当已启用时）
/// </summary>
GlobalPrompt? GetGlobalPromptById(string id);
```

### 新增类

```csharp
// 位置: STranslate.Plugin/GlobalPrompt.cs
public partial class GlobalPrompt : ObservableObject
{
    public string Id { get; set; }         // GUID
    public string Name { get; set; }       // 名称
    public bool IsEnabled { get; set; }    // 是否启用
    public ObservableCollection<PromptItem> Items { get; set; }
    
    public Prompt ToPrompt(bool isEnabled = false);
    public GlobalPrompt Clone();
}
```

---

## 插件使用示例

```csharp
// 获取已启用的全局提示词
var globalPrompts = Context.GetGlobalPrompts();

// 使用特定提示词
var prompt = globalPrompts.First().ToPrompt(true);
Prompts.Add(prompt);
```

---

## 文件变更

### 新增

- `src/STranslate.Plugin/GlobalPrompt.cs` - 全局提示词模型
- `src/STranslate/Views/GlobalPromptEditWindow.xaml` - 编辑窗口UI
- `src/STranslate/Views/GlobalPromptEditWindow.xaml.cs`
- `src/STranslate/ViewModels/Pages/GlobalPromptViewModel.cs`
- `src/STranslate/ViewModels/SelectableGlobalPrompt.cs`
- `src/docs/global-prompts.md` - 功能文档

### 修改

- `src/STranslate.Plugin/IPluginContext.cs` - 新增接口方法
- `src/STranslate.Plugin/Prompt.cs` - 添加 Tag 属性
- `src/STranslate/Core/PluginContext.cs` - 实现接口方法
- `src/STranslate/Core/Settings.cs` - 添加 GlobalPrompts 集合
- `src/STranslate/Views/Pages/TranslatePage.xaml` - 添加编辑按钮
- `src/STranslate/Views/SettingsWindow.xaml` - 调整窗口宽度
- `src/STranslate/Languages/zh-cn.xaml` - 添加语言资源
- `src/STranslate/Languages/en.xaml`

---

## 用户入口

**设置 → 服务 → 文本翻译 → (页面右上角)全局提示词：[编辑]**

---

## 设计原则

1. **接口暴露**：插件通过接口主动获取，而非自动注入
2. **明确分离**：全局和局部提示词分开管理，各管各的
3. **启用控制**：开关控制是否暴露给插件，禁用的不返回
4. **向后兼容**：不影响现有插件，插件可选择性使用

---

## 测试情况

- [x] 编译通过（0错误0警告）
- [x] 编辑窗口正常打开
- [x] 主题跟随正常
- [x] 开关控制暴露正常
- [x] 接口返回过滤正常

---

## 相关文档

详细文档请参阅：`src/docs/global-prompts.md`
