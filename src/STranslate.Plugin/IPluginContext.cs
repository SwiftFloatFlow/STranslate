using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace STranslate.Plugin;

/// <summary>
/// 提供插件设置存储的加载和保存功能的接口。
/// </summary>
public interface IPluginContext : IDisposable
{
    /// <summary>
    /// 插件元数据
    /// </summary>
    PluginMetaData MetaData { get; }

    /// <summary>
    /// 日志
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// 获取翻译
    /// </summary>
    string GetTranslation(string key);

    /// <summary>
    /// Http服务
    /// </summary>
    IHttpService HttpService {get; }

    /// <summary>
    /// 音频播放
    /// </summary>
    IAudioPlayer AudioPlayer { get; }

    /// <summary>
    /// 消息弹窗
    /// </summary>
    ISnackbar Snackbar { get; }

    /// <summary>
    /// 通知
    /// </summary>
    INotification Notification { get; }

    /// <summary>
    /// 图片质量
    /// </summary>
    ImageQuality ImageQuality { get; }

    /// <summary>
    /// 获取Prompt编辑窗口
    /// </summary>
    /// <param name="prompts"></param>
    /// <param name="roles"></param>
    /// <returns></returns>
    Window GetPromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default);

    /// <summary>
    /// 加载插件的设置存储。
    /// </summary>
    /// <typeparam name="T">设置存储的类型。</typeparam>
    /// <returns>设置存储对象。</returns>
    T LoadSettingStorage<T>() where T : new();

    /// <summary>
    /// 保存插件的设置存储。
    /// </summary>
    /// <typeparam name="T">设置存储的类型。</typeparam>
    void SaveSettingStorage<T>() where T : new();

    /// <summary>
    /// 将当前应用主题应用到指定窗口，使插件窗口与主程序保持一致的视觉风格。
    /// </summary>
    /// <param name="window">需要应用主题的窗口实例</param>
    void ApplyTheme(Window window);

    /// <summary>
    /// 获取所有全局提示词列表（只读）。
    /// 插件可以从中选择需要的提示词，但不应该修改这些提示词。
    /// </summary>
    /// <returns>全局提示词的只读列表</returns>
    IReadOnlyList<GlobalPrompt> GetGlobalPrompts();

    /// <summary>
    /// 根据ID获取特定的全局提示词。
    /// </summary>
    /// <param name="id">全局提示词的唯一标识</param>
    /// <returns>匹配的全局提示词，如果不存在则返回null</returns>
    GlobalPrompt? GetGlobalPromptById(string id);

    /// <summary>
    /// 注册全局提示词变更回调。
    /// 当全局提示词发生添加、删除、修改或启用状态变化时，主软件会调用此回调通知插件。
    /// </summary>
    /// <param name="callback">回调函数，参数为变更后的全局提示词只读列表</param>
    /// <param name="lifetime">回调的生命周期管理器。如果提供，当管理器被释放时自动注销回调</param>
    /// <returns>用于手动注销回调的句柄，可调用 Dispose 注销。如果提供了 lifetime 参数，则无需手动调用</returns>
    IDisposable RegisterGlobalPromptsChangedCallback(Action<IReadOnlyList<GlobalPrompt>> callback, IDisposable? lifetime = null);

    /// <summary>
    /// 注销全局提示词变更回调。
    /// 插件卸载时应调用此方法以避免内存泄漏。
    /// 注意：如果注册时提供了 lifetime 参数，回调会在 lifetime 释放时自动注销，无需手动调用此方法。
    /// </summary>
    /// <param name="callback">之前注册的回调函数</param>
    void UnregisterGlobalPromptsChangedCallback(Action<IReadOnlyList<GlobalPrompt>> callback);
}