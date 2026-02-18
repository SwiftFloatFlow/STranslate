using STranslate.Plugin;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace STranslate.Core;

/// <summary>
/// 全局提示词管理器 - 负责管理全局提示词生命周期和同步
/// </summary>
public class GlobalPromptManager : IDisposable
{
    private readonly Settings _settings;
    private readonly ServiceManager _serviceManager;
    private readonly Dictionary<Service, NotifyCollectionChangedEventHandler> _serviceHandlers = new();
    private bool _isDisposed = false;

    public GlobalPromptManager(Settings settings, ServiceManager serviceManager)
    {
        _settings = settings;
        _serviceManager = serviceManager;

        // 监听全局提示词集合变化
        _settings.GlobalPrompts.CollectionChanged += OnGlobalPromptsChanged;
        
        // 监听服务添加事件
        _serviceManager.ServiceAdded += OnServiceAdded;
    }

    /// <summary>
    /// 全局提示词变更处理
    /// </summary>
    private void OnGlobalPromptsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 如果是删除操作，清理相关状态
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
        {
            CleanupDeletedGlobalPrompts(e.OldItems.Cast<Models.GlobalPrompt>());
        }
        
        // 重新合并所有AI服务
        ReMergeAllAiServices();
    }

    /// <summary>
    /// 清理已删除全局提示词的状态
    /// </summary>
    private void CleanupDeletedGlobalPrompts(IEnumerable<Models.GlobalPrompt> deletedPrompts)
    {
        var deletedIds = deletedPrompts.Select(p => p.Id).ToHashSet();
        
        foreach (var service in _serviceManager.Services)
        {
            if (service.Options?.GlobalPromptStates == null) continue;
            
            foreach (var id in deletedIds)
            {
                // 从启用状态字典中移除
                service.Options.GlobalPromptStates.Remove(id);
                
                // 从引用列表中移除
                service.Options.ReferencedGlobalPromptIds.Remove(id);
            }
        }
    }

    /// <summary>
    /// 新服务添加处理
    /// </summary>
    private void OnServiceAdded(object? sender, Service service)
    {
        if (service.Plugin is not LlmTranslatePluginBase)
            return;

        // 注册引用变更监听
        RegisterService(service);
        
        // 执行初始合并
        ServicePromptMerger.MergeGlobalPrompts(service, _settings);
    }

    /// <summary>
    /// 注册服务引用变更监听
    /// </summary>
    public void RegisterService(Service service)
    {
        if (service.Options == null || _serviceHandlers.ContainsKey(service)) 
            return;

        var handler = new NotifyCollectionChangedEventHandler(
            (s, e) => OnServiceReferencesChanged(service));
        
        service.Options.ReferencedGlobalPromptIds.CollectionChanged += handler;
        _serviceHandlers[service] = handler;
    }

    /// <summary>
    /// 服务引用变更处理
    /// </summary>
    private void OnServiceReferencesChanged(Service service)
    {
        ServicePromptMerger.ReMergeGlobalPrompts(service, _settings);
    }

    /// <summary>
    /// 重新合并所有AI服务
    /// </summary>
    public void ReMergeAllAiServices()
    {
        var aiServices = _serviceManager.Services
            .Where(s => s.Plugin is LlmTranslatePluginBase)
            .ToList();

        foreach (var service in aiServices)
        {
            ServicePromptMerger.ReMergeGlobalPrompts(service, _settings);
        }
    }

    /// <summary>
    /// 初始化所有服务的合并状态
    /// </summary>
    public void InitializeAllServices()
    {
        var aiServices = _serviceManager.Services
            .Where(s => s.Plugin is LlmTranslatePluginBase);

        foreach (var service in aiServices)
        {
            RegisterService(service);
            ServicePromptMerger.MergeGlobalPrompts(service, _settings);
        }
    }

    /// <summary>
    /// 保存全局提示词并触发同步
    /// </summary>
    public void SaveGlobalPrompts()
    {
        _settings.Save();
        // CollectionChanged 会自动触发 ReMerge
    }

    #region IDisposable

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        // 取消全局事件订阅
        _settings.GlobalPrompts.CollectionChanged -= OnGlobalPromptsChanged;
        _serviceManager.ServiceAdded -= OnServiceAdded;

        // 取消所有服务的引用监听
        foreach (var kvp in _serviceHandlers)
        {
            if (kvp.Key.Options != null)
            {
                kvp.Key.Options.ReferencedGlobalPromptIds.CollectionChanged -= kvp.Value;
            }
        }
        _serviceHandlers.Clear();

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
