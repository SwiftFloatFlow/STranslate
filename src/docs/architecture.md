# 架构设计

## 架构概览

STranslate 采用插件化架构，核心设计思想是将功能拆分为可独立开发、部署和扩展的插件。

### 核心组件

#### 1. 应用程序启动 (`App.xaml.cs:296-319`)
- 通过 `SingleInstance<App>` 强制单实例
- Velopack 更新检查
- 设置加载（Settings, HotkeySettings, ServiceSettings）
- DI 容器设置（Microsoft.Extensions.Hosting）
- 通过 `PluginManager` 加载插件
- 通过 `ServiceManager` 初始化服务

#### 2. 插件系统 (`Core/PluginManager.cs`)
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

#### 3. 服务管理 (`Core/ServiceManager.cs`)
- `Service` 是包装插件的运行时实例
- 服务从 `PluginMetaData` 创建并存储在 `ServiceData`（持久化配置）中
- 四种服务类型：翻译、OCR、TTS、词汇
- 服务在启动时从设置和插件元数据加载

## 启动流程

### 应用程序启动流程 (`App.xaml.cs:296-319`)

```
App.OnStartup()
├── SingleInstance<App> 强制单实例
├── Velopack 更新检查
├── 设置加载
│   ├── Settings.json - 通用设置
│   ├── HotkeySettings.json - 快捷键配置
│   └── ServiceSettings.json - 服务配置
├── DI 容器设置（Microsoft.Extensions.Hosting）
├── PluginManager.LoadPlugins() - 加载插件
└── ServiceManager.LoadServices() - 初始化服务
```

### 详细步骤

1. **强制单实例**
   - 使用 `SingleInstance<App>` 确保只有一个应用实例运行

2. **Velopack 更新检查**
   - 检查应用更新
   - 自动下载并安装更新（如有）

3. **设置加载**
   - `Settings` - 通用应用程序设置
   - `HotkeySettings` - 热键配置，包含全局热键和软件内热键
   - `ServiceSettings` - 服务配置（启用、顺序、选项）

4. **DI 容器设置**
   - 使用 Microsoft.Extensions.Hosting
   - 注册核心服务（TranslateService, OcrService, TtsService, VocabularyService）
   - 注册视图模型

5. **插件加载**
   - `PluginManager.LoadPlugins()` 扫描插件目录
   - 从 `plugin.json` 提取元数据
   - 通过 `PluginAssemblyLoader` 加载程序集
   - 查找 `IPlugin` 实现
   - 创建带类型信息的 `PluginMetaData`

6. **服务初始化**
   - `ServiceManager.LoadServices()` 遍历设置中的每个 `ServiceData`
   - 与 `PluginMetaData` 匹配
   - 创建 `Service` 实例
   - 调用 `Service.Initialize()` → `Plugin.Init(IPluginContext)`

## 插件系统

### 插件加载机制 (`Core/PluginManager.cs`)

#### 插件来源

| 目录 | 说明 | 位置 |
|------|------|------|
| `PreinstalledDirectory` | 内置插件 | `Plugins/` 文件夹 |
| `PluginsDirectory` | 用户安装的插件 | `%APPDATA%\STranslate\Plugins\` |

#### 插件包格式

每个插件是一个 `.spkg` 文件（ZIP 压缩包），包含：
```
plugin.json          # 元数据
YourPlugin.dll       # 主程序集
icon.png            # 可选图标
Languages/*.xaml     # 可选 i18n 文件
```

#### 插件类型

插件实现 `IPlugin` 接口及其子类型：

| 接口 | 功能 | 基类 |
|------|------|------|
| `ITranslatePlugin` | 翻译服务 | `TranslatePluginBase` / `LlmTranslatePluginBase` |
| `IOcrPlugin` | OCR 服务 | `OcrPluginBase` |
| `ITtsPlugin` | 文本转语音 | `TtsPluginBase` |
| `IVocabularyPlugin` | 词汇管理 | `VocabularyPluginBase` |
| `IDictionaryPlugin` | 字典查询 | - |

#### 插件生命周期

```
PluginManager.LoadPlugins()
→ 扫描插件目录（PreinstalledDirectory + PluginsDirectory）
→ 发现 .spkg 文件
→ 从 plugin.json 提取元数据
→ 通过 PluginAssemblyLoader 加载程序集
→ 使用 MetadataLoadContext 反射查找 IPlugin 实现
→ 创建带类型信息的 PluginMetaData
```

#### 程序集加载

- 使用 `PluginAssemblyLoader` 和 `System.Reflection.MetadataLoadContext`
- 支持插件隔离加载
- 避免程序集冲突

### 插件元数据 (`PluginMetaData`)

```csharp
public class PluginMetaData
{
    public string PluginID { get; set; }      // 唯一标识符
    public string Name { get; set; }          // 显示名称
    public string Author { get; set; }        // 作者
    public string Version { get; set; }       // 版本
    public string Description { get; set; }   // 描述
    public string Website { get; set; }       // 网站
    public string ExecuteFileName { get; set; } // DLL 文件名
    public string IconPath { get; set; }      // 图标路径
    public Type PluginType { get; set; }      // 插件类型（ITranslatePlugin/IOcrPlugin 等）
}
```

## 服务管理

### Service 与 Plugin 的关系

理解 **Service** 和 **Plugin** 的区别至关重要：

| 概念 | 说明 | 类比 |
|------|------|------|
| **Plugin (`PluginMetaData`)** | 插件的类型定义，包含程序集信息、元数据 | 类（Class） |
| **Service** | 插件的运行时实例，拥有独立的配置、状态和服务 ID | 对象实例（Object） |

#### 关键区别

- 同一 **Plugin** 类型可被多个 **Service** 共享使用
- 多个 **Service** 可使用同一 **Plugin** 类型（如两个不同 API Key 的百度翻译服务）
- 每个 **Service** 拥有独立的 `ServiceID`（GUID）和配置

### Service 创建流程

`ServiceManager.CreateService()` 的工作流程：

```
1. 克隆 PluginMetaData（每个 Service 有自己的副本）
2. 创建或重用 ServiceID（GUID）
3. 通过 Activator.CreateInstance() 创建插件实例
4. 创建 PluginContext 提供给插件
5. 组装 Service 对象，包含：
   - Plugin（插件实例）
   - MetaData（元数据副本）
   - Context（插件上下文）
   - Options（服务选项）
```

### 服务加载流程

```
ServiceManager.LoadServices()
→ 遍历 ServiceSettings 中的每个 ServiceData
→ 与 PluginMetaData 匹配（通过 PluginID）
→ 如果匹配成功，创建 Service 实例
→ 调用 Service.Initialize()
   → Plugin.Init(IPluginContext)
      → 插件获取上下文，初始化自身
```

### Service 数据结构

```csharp
public class Service
{
    public IPlugin Plugin { get; }           // 插件实例
    public PluginMetaData MetaData { get; }  // 元数据
    public IPluginContext Context { get; }   // 插件上下文
    public ServiceOptions Options { get; }   // 服务选项（启用、自动执行等）
    public Guid ServiceID { get; }           // 服务唯一标识
}
```

### 服务类型

四种核心服务类型：

| 服务类型 | 管理器 | 接口 |
|---------|--------|------|
| 翻译 | `TranslateService` | `ITranslatePlugin` |
| OCR | `OcrService` | `IOcrPlugin` |
| TTS | `TtsService` | `ITtsPlugin` |
| 词汇 | `VocabularyService` | `IVocabularyPlugin` |

## 关键接口

### IPlugin (基础接口)

所有插件必须实现的基础接口：

```csharp
public interface IPlugin : IDisposable
{
    // 使用上下文初始化插件
    void Init(IPluginContext context);

    // 返回设置界面 UserControl
    UserControl GetSettingUI();

    // 清理资源
    void Dispose();
}
```

### IPluginContext (提供给插件的上下文)

插件通过上下文访问主应用程序功能：

```csharp
public interface IPluginContext
{
    // 元数据
    PluginMetaData MetaData { get; }

    // 日志
    ILogger Logger { get; }

    // HTTP 服务（支持代理）
    IHttpService HttpService { get; }

    // 音频播放
    IAudioPlayer AudioPlayer { get; }

    // 提示消息
    ISnackbar Snackbar { get; }

    // 系统通知
    INotification Notification { get; }

    // 持久化存储（自动定位到插件专属目录）
    T LoadSettingStorage<T>() where T : class, new();
    void SaveSettingStorage<T>(T settings) where T : class;

    // i18n 支持
    string GetTranslation(string key);
}
```

### ITranslatePlugin (翻译插件)

```csharp
public interface ITranslatePlugin : IPlugin
{
    // 核心翻译方法
    Task TranslateAsync(
        TranslateRequest request,
        TranslateResult result,
        CancellationToken cancellationToken);

    // 语言映射
    string GetSourceLanguage(string language);
    string GetTargetLanguage(string language);

    // 结果属性（双向绑定）
    TranslateResult TransResult { get; set; }
    TranslateResult TransBackResult { get; set; }
}
```

### IOcrPlugin (OCR 插件)

```csharp
public interface IOcrPlugin : IPlugin
{
    // 图像转文本
    Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken);

    // 支持的语言列表
    IReadOnlyList<string> SupportedLanguages { get; }
}
```

### ITtsPlugin (TTS 插件)

```csharp
public interface ITtsPlugin : IPlugin
{
    // 文本转语音
    Task SpeakAsync(
        TtsRequest request,
        CancellationToken cancellationToken);

    // 停止播放
    void Stop();
}
```

### IVocabularyPlugin (词汇插件)

```csharp
public interface IVocabularyPlugin : IPlugin
{
    // 保存单词到生词本
    Task SaveAsync(VocabularyEntry entry);

    // 查询生词本
    Task<IReadOnlyList<VocabularyEntry>> QueryAsync();
}
```

### 插件基类

#### TranslatePluginBase

提供常用功能：
- 语言映射
- 结果处理
- HTTP 请求辅助

#### LlmTranslatePluginBase

继承自 `TranslatePluginBase`，专为 LLM 服务设计：
- 提示词编辑
- 流式响应处理
- 对话历史管理

## 数据流：翻译示例

以翻译功能为例，说明数据如何在系统中流动。

### 完整数据流图

```
用户触发翻译（快捷键/UI）
    ↓
MainWindowViewModel.TranslateCommand
    ↓
翻译准备阶段
    ├── 取消进行中的操作
    ├── 重置所有服务状态（ResetAllServices()）
    └── 语言检测（LanguageDetector.GetLanguageAsync()）
    ↓
获取激活服务
    └── TranslateService 返回 ExecutionMode.Automatic 的启用服务
    ↓
并行执行翻译（ExecuteTranslationForServicesAsync）
    ├── 使用 SemaphoreSlim 限制并发数（ProcessorCount * 10）
    └── 对每个 Service 调用 ExecuteTranslationHandlerAsync
    ↓
插件执行（ExecuteAsync）
    ├── plugin.Reset()
    ├── plugin.TransResult.IsProcessing = true
    └── await plugin.TranslateAsync(...)
    ↓
插件内部处理
    ├── Context.LoadSettingStorage<Settings>() 获取配置
    ├── Context.HttpService 发起 HTTP 请求（支持代理）
    └── 解析响应，调用 result.Success(text) 或 result.Fail(message)
    ↓
结果处理
    ├── TranslateResult 是 ObservableObject，自动更新 UI
    ├── 如启用回译，调用 ExecuteBackAsync()
    └── 保存到历史数据库（SqlService）
```

### 详细步骤

#### 1. 触发翻译

用户通过以下方式触发翻译：
- 全局热键（如 `Alt + D` 划词翻译）
- 主界面输入框 + 翻译按钮
- 剪贴板监听自动触发

入口：`MainWindowViewModel.TranslateCommand`

#### 2. 翻译准备

```csharp
// 取消之前的翻译任务
_cancellationTokenSource?.Cancel();
_cancellationTokenSource = new CancellationTokenSource();

// 重置所有服务状态
ResetAllServices();

// 检测源语言（如设置为自动）
var detectedLanguage = await LanguageDetector.GetLanguageAsync(inputText);
```

#### 3. 获取激活服务

```csharp
// 获取所有启用的翻译服务
var services = TranslateService.GetServices()
    .Where(s => s.Options.IsEnabled && s.Options.ExecutionMode == ExecutionMode.Automatic);
```

#### 4. 并行执行

```csharp
// 限制并发数，避免过多请求
var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 10);

var tasks = services.Select(async service =>
{
    await semaphore.WaitAsync(cancellationToken);
    try
    {
        await ExecuteTranslationHandlerAsync(service, request, cancellationToken);
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);
```

#### 5. 插件执行

```csharp
// 在 Service 类中
public async Task ExecuteAsync(TranslateRequest request, CancellationToken cancellationToken)
{
    // 重置插件状态
    Plugin.Reset();

    // 标记为处理中
    Plugin.TransResult.IsProcessing = true;

    // 执行翻译
    await Plugin.TranslateAsync(request, Plugin.TransResult, cancellationToken);
}
```

#### 6. 插件内部处理

```csharp
public class MyTranslatePlugin : TranslatePluginBase
{
    public override async Task TranslateAsync(
        TranslateRequest request,
        TranslateResult result,
        CancellationToken cancellationToken)
    {
        // 1. 加载设置（API Key 等）
        var settings = Context.LoadSettingStorage<Settings>();

        // 2. 构建请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, settings.ApiUrl);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { text = request.Text }),
            Encoding.UTF8, "application/json");

        // 3. 发送请求（自动使用系统代理）
        var response = await Context.HttpService.SendAsync(
            httpRequest, cancellationToken);

        // 4. 解析响应
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Response>(content);

        // 5. 设置结果（自动更新 UI）
        if (data.Success)
            result.Success(data.TranslatedText);
        else
            result.Fail(data.ErrorMessage);
    }
}
```

#### 7. 结果处理

- `TranslateResult` 继承自 `ObservableObject`，结果变更自动通知 UI
- 如果启用回译，对目标文本再次调用翻译（方向相反）
- 保存到 SQLite 历史数据库：`%APPDATA%\STranslate\Cache\history.db`

### 线程安全

- 翻译请求使用 `SemaphoreSlim` 控制并发（默认 `ProcessorCount * 10`）
- 所有插件操作支持 `CancellationToken` 取消
- 每个插件实例是独立的，线程安全由各自实现保证
