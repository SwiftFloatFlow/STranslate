# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 在处理本仓库代码时提供指导。

## 语言规则
- 所有解释、推理和评论必须用简体中文书写。
- 除非是代码、标识符或不可避免的技术术语，否则不得使用英语。
- 错误解释和总结必须用中文。

## 文档导航

本文档已按功能模块拆分为以下子文档：

### 快速开始
- [**项目概述**](docs/overview.md) - STranslate 项目简介、主要功能、构建命令

### 架构设计
- [**架构设计**](docs/architecture.md) - 核心架构说明
  - 启动流程 - 应用程序启动过程
  - 插件系统 - 插件加载与管理
  - 服务管理 - Service 与 Plugin 的关系
  - 关键接口 - IPlugin、IPluginContext 等接口定义
  - 数据流 - 翻译功能的数据流示例

### 功能特性
- [**功能特性**](docs/features.md) - 热键系统、剪贴板监听、历史记录

### 存储与配置
- [**存储与配置**](docs/storage.md) - 设置架构、存储位置

### 插件开发
- [**插件开发指南**](docs/plugin.md) - 插件开发、包格式、社区插件开发

### 开发参考
- [**常见开发任务**](docs/development.md) - 修改核心服务、UI 更改、调试插件
- [**参考信息**](docs/reference.md) - 关键文件索引、技术栈与依赖项

## 快速参考

| 任务 | 相关文档 |
|------|---------|
| 了解项目结构 | [项目概述](docs/overview.md) |
| 构建项目 | [项目概述](docs/overview.md) |
| 开发插件 | [插件开发指南](docs/plugin.md) |
| 修改热键功能 | [功能特性](docs/features.md) |
| 修改剪贴板监听 | [功能特性](docs/features.md) |
| 修改历史记录 | [功能特性](docs/features.md) |
| 修改核心服务 | [常见开发任务](docs/development.md) |
| 查找关键文件 | [参考信息](docs/reference.md) |
