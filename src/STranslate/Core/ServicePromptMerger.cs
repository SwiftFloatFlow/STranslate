using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using STranslate.Models;
using STranslate.Plugin;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;

namespace STranslate.Core;

/// <summary>
/// 服务提示词合并器 - 负责将全局提示词注入到AI翻译服务
/// </summary>
public static class ServicePromptMerger
{
    // V4.0: 读写锁，保证线程安全
    private static readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    
    // V4.0: 缓存 PropertyInfo ，避免重复反射
    private static readonly Dictionary<Type, PropertyInfo?> _promptsPropertyCache = new();

    // 日志记录器（延迟初始化）
    private static ILogger? _logger;
    private static ILogger Logger => _logger ??= Ioc.Default.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ServicePromptMerger));

    /// <summary>
    /// 初始化时合并全局提示词（线程安全）
    /// </summary>
    public static void MergeGlobalPrompts(Service service, Settings globalSettings)
    {
        if (service.Plugin is not LlmTranslatePluginBase llmPlugin)
            return;

        var referencedIds = service.Options?.ReferencedGlobalPromptIds;
        if (referencedIds == null || referencedIds.Count == 0)
            return;

        // 在UI线程执行，并加写锁
        Application.Current.Dispatcher.Invoke(() =>
        {
            _lock.EnterWriteLock();
            try
            {
                DoMerge(llmPlugin, service.Options!, globalSettings.GlobalPrompts);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// 重新合并全局提示词（保留启用状态，线程安全）
    /// </summary>
    public static void ReMergeGlobalPrompts(Service service, Settings globalSettings)
    {
        if (service.Plugin is not LlmTranslatePluginBase llmPlugin)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _lock.EnterWriteLock();
            try
            {
                // 1. 保存当前启用状态
                SaveEnabledStates(llmPlugin, service.Options!);
                
                // 2. 移除所有全局提示词
                RemoveAllGlobalPrompts(llmPlugin);
                
                // 3. 重新合并
                DoMerge(llmPlugin, service.Options!, globalSettings.GlobalPrompts);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// 执行合并操作
    /// </summary>
    private static void DoMerge(
        LlmTranslatePluginBase llmPlugin, 
        TranslationOptions options,
        ObservableCollection<GlobalPrompt> globalPrompts)
    {
        var referencedIds = options.ReferencedGlobalPromptIds;
        if (referencedIds.Count == 0) return;

        foreach (var globalPrompt in globalPrompts)
        {
            // 检查是否被引用
            if (!referencedIds.Contains(globalPrompt.Id))
                continue;

            // 检查是否已存在（通过ID精确匹配）
            if (llmPlugin.Prompts.Any(p => ExtractGlobalId(p) == globalPrompt.Id))
                continue;

            // 恢复启用状态
            bool isEnabled = options.GlobalPromptStates.TryGetValue(globalPrompt.Id, out var state) 
                ? state 
                : false;

            // 创建并添加
            var prompt = globalPrompt.ToPrompt(isEnabled);
            llmPlugin.Prompts.Add(prompt);
        }
    }

    /// <summary>
    /// 保存当前已启用的全局提示词状态
    /// </summary>
    private static void SaveEnabledStates(LlmTranslatePluginBase llmPlugin, TranslationOptions options)
    {
        foreach (var prompt in llmPlugin.Prompts.Where(IsGlobalPrompt))
        {
            var id = ExtractGlobalId(prompt);
            if (!string.IsNullOrEmpty(id))
            {
                options.GlobalPromptStates[id] = prompt.IsEnabled;
            }
        }
    }

    /// <summary>
    /// 移除所有全局提示词
    /// </summary>
    private static void RemoveAllGlobalPrompts(LlmTranslatePluginBase llmPlugin)
    {
        var toRemove = llmPlugin.Prompts.Where(IsGlobalPrompt).ToList();
        foreach (var prompt in toRemove)
        {
            llmPlugin.Prompts.Remove(prompt);
        }
    }

    #region 识别与提取

    /// <summary>
    /// 判断是否为全局提示词（支持新旧格式）
    /// </summary>
    public static bool IsGlobalPrompt(Prompt prompt)
    {
        if (prompt == null) return false;
        
        // 第一优先级：通过 Tag 判断
        if (prompt.Tag is string tag && tag.StartsWith("Global:"))
            return true;
        
        // 第二优先级：通过名称前缀判断
        return prompt.Name.StartsWith("[Global:") || prompt.Name.StartsWith("[Global]");
    }

    /// <summary>
    /// 从 Prompt 中提取全局提示词ID（双重识别）
    /// </summary>
    /// <returns>全局提示词ID，如果不是全局提示词或无法识别则返回 null</returns>
    public static string? ExtractGlobalId(Prompt prompt)
    {
        if (prompt == null) return null;
        
        // 第一优先级：从 Tag 提取（最可靠）
        if (prompt.Tag is string tag && tag.StartsWith("Global:"))
            return tag.Substring(7);
        
        // 第二优先级：从名称解析 [Global:ID] 格式
        // V4.0 修正: 支持带连字符的GUID格式 [a-f0-9-]+
        if (prompt.Name.StartsWith("[Global:"))
        {
            var match = Regex.Match(prompt.Name, @"^\[Global:([a-f0-9-]+)\]");
            if (match.Success)
                return match.Groups[1].Value;
        }
        
        // 第三优先级：旧格式 [Global] 名称（无ID）
        if (prompt.Name.StartsWith("[Global]"))
            return null;  // 无法精确识别ID
        
        return null;
    }

    /// <summary>
    /// 获取显示名称（去掉 [Global:ID] 前缀）
    /// </summary>
    public static string GetDisplayName(Prompt prompt)
    {
        if (prompt == null) return string.Empty;
        
        var name = prompt.Name;
        
        // 去掉 [Global:ID] 前缀
        if (name.StartsWith("[Global:"))
        {
            var endIndex = name.IndexOf("] ");
            if (endIndex > 0)
                return name.Substring(endIndex + 2);
        }
        
        // 旧格式 [Global] 名称
        if (name.StartsWith("[Global] "))
            return name.Substring(9);
        
        return name;
    }

    #endregion

    #region 过滤与保存

    /// <summary>
    /// 尝试从设置中过滤掉全局提示词（健壮性版本）
    /// </summary>
    /// <typeparam name="T">设置类型</typeparam>
    /// <param name="settings">设置对象</param>
    /// <param name="filteredPrompts">过滤后的提示词列表</param>
    /// <returns>是否成功过滤</returns>
    public static bool TryFilterGlobalPrompts<T>(T settings, out List<Prompt> filteredPrompts) 
        where T : new()
    {
        filteredPrompts = new List<Prompt>();
        
        try
        {
            var type = typeof(T);
            
            // 从缓存获取 PropertyInfo
            if (!_promptsPropertyCache.TryGetValue(type, out var promptsProperty))
            {
                promptsProperty = type.GetProperty("Prompts", 
                    BindingFlags.Public | BindingFlags.Instance);
                _promptsPropertyCache[type] = promptsProperty;
            }
            
            if (promptsProperty == null)
            {
                Logger.LogDebug("Settings 类型 {TypeName} 没有 Prompts 属性，跳过过滤", type.Name);
                return false;
            }

            // 检查类型是否为 ObservableCollection<Prompt>
            if (!typeof(ObservableCollection<Prompt>).IsAssignableFrom(promptsProperty.PropertyType))
            {
                Logger.LogDebug("Settings.Prompts 类型不是 ObservableCollection<Prompt>，跳过过滤");
                return false;
            }

            // 获取值
            if (promptsProperty.GetValue(settings) is not ObservableCollection<Prompt> prompts)
                return false;

            // 过滤并克隆
            filteredPrompts = prompts
                .Where(p => !IsGlobalPrompt(p))
                .Select(p => p.Clone())
                .ToList();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "过滤全局提示词失败");
            return false;
        }
    }

    #endregion
}
