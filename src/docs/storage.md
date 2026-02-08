# 存储与配置

## 设置架构 (`StorageBase<T>`)

- JSON 序列化
- 原子写入（`.tmp` + `.bak` 备份）
- 自动备份机制

## 存储位置

位于 `DataLocation.DataDirectory()`：

| 模式 | 路径 |
|------|------|
| 便携模式 | `./PortableConfig/` |
| 漫游模式 | `%APPDATA%\STranslate\` |

## 主要设置文件

| 文件 | 用途 |
|------|------|
| `Settings.json` | 通用设置 |
| `HotkeySettings.json` | 快捷键配置 |
| `ServiceSettings.json` | 服务配置（启用、顺序、选项） |

## 插件存储

### 设置存储

路径格式：`%APPDATA%\STranslate\Settings\Plugins\{PluginName}_{PluginID}\{ServiceID}.json`

### 缓存存储

路径格式：`%APPDATA%\STranslate\Cache\Plugins\{PluginName}_{PluginID}\`

### 访问方式

通过 `IPluginContext` 访问，无需关心具体路径：

```csharp
// 加载设置
var settings = Context.LoadSettingStorage<Settings>();

// 保存设置
Context.SaveSettingStorage(settings);
```

## 历史数据库

SQLite 数据库：`%APPDATA%\STranslate\Cache\history.db`

- 保存翻译历史
- 支持搜索和回顾
