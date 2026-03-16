# 插件 SDK 与开发范式

## 模块职责
- 定义插件开发接口、基础模型与运行时上下文能力。
- 说明插件从 `plugin.json` 到运行时服务实例的生命周期。
- 提供官方插件实现的共性范式，指导新插件快速落地。

## 关键入口
- `STranslate.Plugin/IPlugin.cs`
  - 所有插件共同入口：`Init(IPluginContext)`、`GetSettingUI()`、`Dispose()`。
- `STranslate.Plugin/IPluginContext.cs`
  - 插件可用能力：`HttpService`、`Logger`、`AudioPlayer`、`Snackbar`、`Notification`、配置存储、主题应用。
- `STranslate.Plugin/ITranslatePlugin.cs`
  - 翻译/词典接口与基类：`TranslatePluginBase`、`LlmTranslatePluginBase`、`DictionaryPluginBase`。
- `STranslate.Plugin/IOcrPlugin.cs`、`ITtsPlugin.cs`、`IVocabularyPlugin.cs`
  - OCR/TTS/生词本插件接口。
- `STranslate.Plugin/PluginMetaData.cs`、`Service.cs`
  - 元数据模型与运行时服务模型。
- 官方插件样例
  - `Plugins/*/Main.cs`
  - `Plugins/*/plugin.json`

## 核心流程
### 从入口到结果：插件被加载并可调用
1. 插件目录包含 `plugin.json`、执行 dll、图标资源。
2. `PluginManager` 读取 `plugin.json`，加载程序集并定位实现 `IPlugin` 的类型。
3. `ServiceManager` 基于 `PluginMetaData` 创建 `Service`：绑定 `Plugin` 实例与 `PluginContext`。
4. `Service.Initialize()` 调用 `Plugin.Init(Context)`，插件读取自身配置并准备可执行状态。
5. UI 层在设置页请求 `GetSettingUI()`，插件返回自身配置面板控件。

### 从入口到结果：插件配置读写
1. 插件 `Init` 内调用 `context.LoadSettingStorage<T>()` 读取配置。
2. 修改配置后调用 `context.SaveSettingStorage<T>()` 持久化。
3. 服务销毁时由 `Service.Dispose()` 调用 `Context.Dispose()` 与 `Plugin.Dispose()` 释放资源。

## 关键数据结构/配置
### `plugin.json` 规范
- 必填字段（与 `PluginMetaData` 对应）：
  - `PluginID`：插件唯一 ID（升级与去重依据）。
  - `Name`
  - `Description`
  - `Author`
  - `Version`
  - `Website`
  - `ExecuteFileName`
  - `IconPath`

### SDK 核心模型
- `PluginMetaData`：插件静态元信息 + 运行时路径与类型。
- `Service`：插件实例容器，含 `ServiceID`、`DisplayName`、`Options`。
- `TranslateRequest` / `TranslateResult`、`DictionaryResult`、`OcrResult`、`VocabularyResult`：能力结果模型。

### 接口与基类选择建议
- 文本翻译：优先继承 `TranslatePluginBase`。
- 大模型翻译：优先继承 `LlmTranslatePluginBase`（内置 Prompt 选择机制）。
- 词典类：继承 `DictionaryPluginBase`。
- OCR/TTS/生词本：分别实现 `IOcrPlugin`、`ITtsPlugin`、`IVocabularyPlugin`。

## 关键文件
- `STranslate.Plugin/IPlugin.cs`
- `STranslate.Plugin/IPluginContext.cs`
- `STranslate.Plugin/ITranslatePlugin.cs`
- `STranslate.Plugin/IOcrPlugin.cs`
- `STranslate.Plugin/ITtsPlugin.cs`
- `STranslate.Plugin/IVocabularyPlugin.cs`
- `STranslate.Plugin/PluginMetaData.cs`
- `STranslate.Plugin/Service.cs`
- `Plugins/STranslate.Plugin.Translate.OpenAI/Main.cs`
- `Plugins/STranslate.Plugin.Ocr.OpenAI/Main.cs`
- `Plugins/STranslate.Plugin.Tts.MicrosoftEdge/Main.cs`
- `Plugins/STranslate.Plugin.Vocabulary.Eudict/Main.cs`

## 常见改动任务
- 新建插件：
  1. 新建项目与 `Main.cs`。
  2. 实现目标接口或基类。
  3. 提供 `plugin.json` 与图标。
  4. 在 `Init` 中加载配置并在设置 UI 中可编辑。
- 增加插件能力参数：先扩展插件 `Settings` 模型，再在 `GetSettingUI()` 对应 VM 中读写并调用 `SaveSettingStorage`。
- 处理长任务取消：所有 `TranslateAsync` / `RecognizeAsync` / `SaveAsync` 应尊重 `CancellationToken`。
- 兼容升级：保持 `PluginID` 稳定，升级仅提升 `Version`，避免被识别为新插件。
