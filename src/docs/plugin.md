# 插件开发指南

## 插件类型

| 插件类型 | 基类 | 主要接口 | 说明 |
|---------|------|---------|------|
| 翻译 | `TranslatePluginBase` / `LlmTranslatePluginBase` | `ITranslatePlugin` | 支持 LLM 的使用 `LlmTranslatePluginBase` |
| OCR | `OcrPluginBase` | `IOcrPlugin` | 图像识别 |
| TTS | `TtsPluginBase` | `ITtsPlugin` | 文本转语音 |
| 词汇 | `VocabularyPluginBase` | `IVocabularyPlugin` | 单词查询/管理 |

## 插件基类说明

- `TranslatePluginBase`: 提供常用功能如语言映射、结果处理、HTTP 请求辅助
- `LlmTranslatePluginBase`: 继承自 `TranslatePluginBase`，专为 LLM 服务设计，提供提示词编辑、流式响应处理
- 插件基类位于 `STranslate.Plugin` 项目中，插件项目需引用该包

## 快速开始

1. 选择插件类型（翻译/OCR/TTS/词汇）
2. 基于现有官方插件模板创建项目
3. 实现对应接口
4. 打包为 .spkg 文件
5. 安装测试

## 插件包格式 (.spkg)

`.spkg` 文件是 ZIP 压缩包，是 STranslate 插件的标准分发格式。

### 包结构

```
plugin.json          # 元数据（必需）
YourPlugin.dll       # 主程序集（必需）
icon.png            # 可选图标
Languages/          # 可选多语言文件目录
    ├── zh-cn.xaml
    ├── en.xaml
    └── ...
```

### plugin.json 规范

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

#### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `PluginID` | string | 是 | 唯一标识符（32位十六进制GUID格式） |
| `Name` | string | 是 | 插件显示名称 |
| `Author` | string | 否 | 作者名称 |
| `Version` | string | 是 | 版本号（如 1.0.0） |
| `Description` | string | 否 | 插件描述 |
| `Website` | string | 否 | 项目网站 |
| `ExecuteFileName` | string | 是 | 主DLL文件名 |
| `IconPath` | string | 否 | 图标文件路径 |

#### PluginID 生成

使用 GUID 生成器创建一个 32 位十六进制字符串：

```bash
# PowerShell
[Guid]::NewGuid().ToString("N")

# 输出示例：d99c702e39b44be5a9e49983ff0f4fff
```

**注意**：每个插件都需要重新生成唯一的 PluginID，重复可能导致插件无法使用。

### 创建 .spkg 文件

```powershell
# 将插件文件打包为 .spkg（ZIP 格式）
Compress-Archive -Path "plugin.json", "YourPlugin.dll", "icon.png", "Languages" -DestinationPath "YourPlugin.spkg"
```

### 安装插件

1. **通过 UI 安装**：设置 → 插件 → 安装 → 选择 .spkg 文件
2. **手动放置**：将 .spkg 文件放入 `%APPDATA%\STranslate\Plugins\` 目录
3. **预安装插件**：放在 `Plugins/` 目录，随应用一起分发

## 社区插件开发指南 (ThirdPlugins)

### 标准开发流程

**核心原则：基于现有插件仓库修改**

#### 1. 找到官方社区插件仓库

- 访问 GitHub: `https://github.com/STranslate/STranslate.Plugin.Translate.{Name}`
- 选择与你需求相似的插件类型（翻译/OCR/TTS/生词本）

#### 2. Clone 仓库

```bash
# Clone 代码
git clone https://github.com/STranslate/STranslate.Plugin.Translate.DeepLX.git
cd STranslate.Plugin.Translate.DeepLX

# 创建自己的 git 仓库
rm -rf .git
git init .

# 重命名操作
# 修改项目文件、命名空间等
```

#### 3. 修改必要信息

- `plugin.json`: 更新 PluginID、Name、Version、Description
- `*.csproj`: 更新项目名称、RepositoryUrl
- `Main.cs`: 修改核心逻辑
- `Settings.cs`: 修改配置模型
- `icon.png`: 替换图标

#### 4. 在主项目中调试（可断点调试）

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

#### 5. 版本管理与发布

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
git tag v1.0.0  # ← 必须以 v开头
git push origin v1.0.0
```

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

## 扩展插件类型

如需添加全新的插件类型（非翻译/OCR/TTS/词汇）：

1. 在 `STranslate.Plugin/` 中定义接口（例如 `IMyPlugin.cs`）
2. 添加到 `ServiceType` 枚举（如果是新的服务类别）
3. 更新 `BaseService.LoadPlugins<T>()` 以加载该类型
4. 更新 `ServiceManager.CreateService()` 以处理该类型
5. 在 `Services/` 中创建服务类（例如 `MyService.cs`）

## 调试插件

### 查看插件加载日志

检查日志文件 `%APPDATA%\STranslate\Logs\{Version}\.log`：
- 插件发现
- 程序集加载
- 类型解析
- 初始化错误

### 测试插件安装

1. 构建插件为 `.spkg`（带 plugin.json 的 ZIP）
2. 使用 UI：设置 → 插件 → 安装
3. 或放在 `Plugins/` 目录作为预安装插件
