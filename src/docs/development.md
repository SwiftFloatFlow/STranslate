# 常见开发任务

## 添加新的插件类型

1. 在 `STranslate.Plugin/` 中定义接口（例如 `IMyPlugin.cs`）
2. 添加到 `ServiceType` 枚举（如果是新的服务类别）
3. 更新 `BaseService.LoadPlugins<T>()` 以加载该类型
4. 更新 `ServiceManager.CreateService()` 以处理该类型
5. 在 `Services/` 中创建服务类（例如 `MyService.cs`）

## 修改核心服务

| 服务 | 文件路径 |
|------|----------|
| TranslateService | `STranslate/Services/TranslateService.cs` |
| OcrService | `STranslate/Services/OcrService.cs` |
| TtsService | `STranslate/Services/TtsService.cs` |
| VocabularyService | `STranslate/Services/VocabularyService.cs` |

## UI 更改

- **Views**: `STranslate/Views/`
- **ViewModels**: `STranslate/ViewModels/`
- **框架**: 使用 CommunityToolkit.Mvvm 进行 MVVM
- **UI 组件**: 使用 iNKORE.UI.WPF.Modern 用于现代 UI 组件

## 调试插件加载

检查日志文件 `%APPDATA%\STranslate\Logs\{Version}\.log`：
- 插件发现
- 程序集加载
- 类型解析
- 初始化错误

## 测试插件安装

1. 构建插件为 `.spkg`（带 plugin.json 的 ZIP）
2. 使用 UI：设置 → 插件 → 安装
3. 或放在 `Plugins/` 目录作为预安装插件

## 添加新的热键

1. 在 `HotkeySettings.cs` 添加热键属性
2. 在热键注册逻辑中添加新热键的注册
3. 如需 UI 设置，更新 `HotkeyPage.xaml`
4. 如需特殊处理逻辑，在 `HotkeyMapper.cs` 或相关帮助类中实现
