# 全局提示词

## 模块职责
- 允许用户在主软件中创建、编辑、删除全局提示词集合。
- 通过 `IPluginContext` 接口将启用的全局提示词暴露给插件使用。
- 提供变更通知机制，支持插件监听全局提示词变化并同步更新。

## 关键入口
- `STranslate.Plugin/Prompt.cs`
  - Prompt 数据模型：`Id`（唯一标识）、`Name`、`Items`、`IsEnabled`。
- `STranslate.Plugin/IPluginContext.cs`
  - 插件可用接口：`GetGlobalPrompts()`、`RegisterGlobalPromptsChangedCallback()`。
- `STranslate/Core/Settings.cs`
  - `GlobalPrompts` 属性、`GlobalPromptsChanged` 事件、`NotifyGlobalPromptsChanged()`。
- `STranslate/Core/PluginContext.cs`
  - 接口实现：返回时重置 `IsEnabled = false`，`CallbackRegistration` 资源管理。
- `STranslate/ViewModels/PromptEditViewModel.cs`
  - 编辑逻辑：`isMutualExclusion` 参数、`HasChanges()` 变更检测、`Dispose()` 防重复调用。
- `STranslate/Views/PromptEditWindow.xaml.cs`
  - 窗口管理：`HasValidSave` 属性、`Closing` 事件资源清理。
- `STranslate/ViewModels/Pages/TranslateViewModel.cs`
  - 全局提示词入口：`EditGlobalPromptsCommand`、有效保存通知。

## 核心流程
### 从入口到结果：插件获取全局提示词
1. 插件 `Init` 阶段调用 `context.GetGlobalPrompts()`。
2. `PluginContext` 从 `Settings.GlobalPrompts` 筛选 `IsEnabled = true` 的提示词。
3. 克隆每个 Prompt 并将 `IsEnabled` 重置为 `false`（避免与插件互斥逻辑冲突）。
4. 返回 `IReadOnlyList<Prompt>` 快照，插件可自由添加到本地提示词列表。

### 从入口到结果：全局提示词编辑与保存
1. 用户在翻译页面点击「编辑」打开 `PromptEditWindow`（`isMutualExclusion: false`）。
2. `PromptEditViewModel` 克隆原始提示词集合进行编辑，监听属性变更。
3. 用户修改后点击保存，`HasChanges()` 通过 JSON 序列化比较检测实际变化。
4. 若有变化：更新原始集合 → 触发 `SaveRequested(true)` → `NotifyGlobalPromptsChanged()` 广播事件。
5. 若无变化：仅关闭窗口，不触发通知。

### 从入口到结果：插件监听全局提示词变更
1. 插件调用 `context.RegisterGlobalPromptsChangedCallback(callback, delayMs)` 注册回调。
2. 主程序保存全局提示词时触发 `GlobalPromptsChanged` 事件。
3. `PluginContext` 筛选启用的提示词，克隆并重置 `IsEnabled = false`，传递给回调。
4. 插件 `Dispose()` 时调用 `IDisposable.Dispose()` 注销回调（支持延时注销防竞态）。

## 关键数据结构/配置
- `Prompt`
  - `Id`：Guid 唯一标识，用于跨组件识别（旧数据反序列化为 `Guid.Empty`）。
  - `Name`：提示词名称。
  - `Items`：`List<PromptItem>`，包含 Role 和 Content。
  - `IsEnabled`：在主软件表示是否暴露给插件，返回给插件时固定为 `false`。
- `PromptItem`
  - `Role`：角色（system/user/assistant）。
  - `Content`：提示词内容。
- `Settings.GlobalPrompts`
  - `ObservableCollection<Prompt>`，集合变更自动触发持久化。
- `CallbackRegistration`（file class）
  - 实现 `IDisposable`，管理回调生命周期和延时注销。

## 关键文件
- `STranslate.Plugin/Prompt.cs`
- `STranslate.Plugin/IPluginContext.cs`
- `STranslate/Core/Settings.cs`
- `STranslate/Core/PluginContext.cs`
- `STranslate/ViewModels/PromptEditViewModel.cs`
- `STranslate/Views/PromptEditWindow.xaml.cs`
- `STranslate/ViewModels/Pages/TranslateViewModel.cs`
- `STranslate/Views/Pages/TranslatePage.xaml`

## 常见改动任务
- 新增全局提示词属性：
  1. 在 `Prompt` 类增加属性并添加 `[JsonPropertyName]`。
  2. 更新 `Clone()` 方法确保新属性被克隆。
  3. 验证 `HasChanges()` 序列化比较能检测到新属性变化。
- 修改通知触发逻辑：
  1. 调整 `HasChanges()` 比较策略（如改用哈希比较优化性能）。
  2. 修改 `NotifyGlobalPromptsChanged()` 筛选条件。
- 扩展插件接口：
  1. 在 `IPluginContext` 新增方法。
  2. 在 `PluginContext` 实现，注意返回值克隆和 `IsEnabled` 重置。
  3. 更新 `CallbackRegistration` 如需支持新事件。

## 兼容性说明
- 旧插件不调用新接口，功能完全不受影响。
- 旧数据反序列化时 `Prompt.Id` 为 `Guid.Empty`，不影响使用。
- `GetGlobalPrompts()` 返回的提示词 `IsEnabled` 固定为 `false`，与插件互斥逻辑解耦。

## 资源管理
- `PromptEditViewModel.Dispose()` 使用 `_disposed` 标志防止重复销毁，确保事件监听只清理一次。
- `PromptEditWindow.OnClosing()` 取消订阅 `Closing` 事件自身，防止窗口实例缓存时内存泄漏。
- 插件应在 `Dispose()` 中调用 `IDisposable.Dispose()` 注销全局提示词变更回调。
