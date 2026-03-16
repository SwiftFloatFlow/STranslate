# OCR 与图片翻译链路

## 模块职责
- 提供截图 OCR、OCR 窗口、图片翻译窗口三条图像文本处理链路。
- 管理 OCR 服务与图片翻译专用 OCR/翻译服务绑定。
- 在图片翻译中完成版面分析、文本块合并、翻译回写与结果图生成。

## 关键入口
- `STranslate/ViewModels/MainWindowViewModel.cs`
  - `ScreenshotTranslateAsync()` / `ScreenshotTranslateHandlerAsync()`
  - `OcrAsync()` / `OcrHandlerAsync()`
  - `ImageTranslateAsync()` / `ImageTranslateHandlerAsync()`
- `STranslate/ViewModels/OcrWindowViewModel.cs`
  - `ExecuteAsync(Bitmap)`：OCR 窗口主执行命令。
- `STranslate/ViewModels/ImageTranslateWindowViewModel.cs`
  - `ExecuteAsync(Bitmap)`：图片翻译窗口主执行命令。
  - `ApplyLayoutAnalysis(OcrResult)`：OCR 文本块空间合并。
- `STranslate/Core/Screenshot.cs`
  - `GetScreenshotAsync()`：截图前隐藏主窗口，调用 `ScreenGrabber`。
- `STranslate/Services/OcrService.cs`
  - `ImageTranslateOcrService`：图片翻译专用 OCR 服务选择与持久化。

## 核心流程
### 从入口到结果：主窗口截图翻译
1. `MainWindowViewModel.ScreenshotTranslateAsync()` 先取可用 OCR 服务。
2. 通过 `IScreenshot.GetScreenshotAsync()` 获取截图位图（主窗口可见且非置顶时先折叠，避免截到自身）。
3. `ScreenshotTranslateHandlerAsync()` 调 OCR `RecognizeAsync()`。
4. OCR 成功后：按设置可复制识别文本，然后调用 `ExecuteTranslate()` 进入主翻译链路。

### 从入口到结果：OCR 窗口执行
1. `OcrWindowViewModel.ExecuteAsync(bitmap)` 设置执行态并清理旧结果。
2. 调用当前启用的 OCR 服务 `RecognizeAsync(new OcrRequest(data, Settings.OcrLanguage))`。
3. 生成两类展示数据：
   - 原图/标注图（边框）
   - `OcrWords` 与 `Result` 文本
4. 根据 `Settings.IsOcrShowingAnnotated` 决定显示原图还是标注图。

### 从入口到结果：图片翻译窗口执行
1. `ImageTranslateWindowViewModel.ExecuteAsync(bitmap)` 获取图片翻译专用 OCR 服务（无则回退启用 OCR 服务）。
2. OCR 后执行 `ApplyLayoutAnalysis()`：按空间相邻关系分组合并文本块。
3. 获取 `TranslateService.ImageTranslateService`（必须是 `ITranslatePlugin`，词典服务不支持）。
4. 并发翻译每个 `OcrContent.Text`，再把译文覆盖回 `OcrResult.OcrContents`。
5. 生成两类图：
   - `_annotatedImage`：合并后边框图
   - `_resultImage`：在原图覆盖译文
6. `Settings.IsImTranShowingAnnotated` 控制最终显示哪种图。

### 图片翻译专用服务绑定
- OCR 专用绑定：`OcrService.ImageTranslateOcrService`，由 `OnSelectedOcrEngineChanged` 写入 `ServiceSettings.ImageTranslateOcrSvcID`。
- 翻译专用绑定：`TranslateService.ImageTranslateService`，由 `OnSelectedTranslateEngineChanged` 写入 `ServiceSettings.ImageTranslateSvcID`。

## 关键数据结构/配置
- `OcrResult` / `OcrContent` / `BoxPoint`：OCR 原始与结构化文本块。
- 版面分析参数（`Settings`）：
  - `VerticalThresholdRatio`
  - `HorizontalThresholdRatio`
  - `LineSpacingThresholdRatio`
  - `WordSpacingThresholdRatio`
- 图像展示设置：
  - `IsOcrShowingAnnotated`
  - `IsImTranShowingAnnotated`
  - `IsImTranShowingTextControl`
  - `ImageQuality`
- OCR 语言设置：`OcrLanguage`

## 关键文件
- `STranslate/ViewModels/MainWindowViewModel.cs`
- `STranslate/ViewModels/OcrWindowViewModel.cs`
- `STranslate/ViewModels/ImageTranslateWindowViewModel.cs`
- `STranslate/Core/Screenshot.cs`
- `STranslate/Services/OcrService.cs`
- `STranslate/Services/TranslateService.cs`
- `STranslate.Plugin/IOcrPlugin.cs`

## 常见改动任务
- 调整版面合并效果：优先改 `ApplyLayoutAnalysis()` 与四个阈值参数，不要只改渲染层。
- 新增图片翻译后处理：应在翻译回写 `content.Text` 后、`GenerateTranslatedImage` 前插入。
- 截图行为改造：在 `Screenshot.GetScreenshotAsync()` 处理窗口折叠与等待时机。
- OCR 服务优先级策略调整：修改 `OcrService.GetImageTranslateOcrServiceOrDefault()` 与对应 VM 的选中逻辑。
