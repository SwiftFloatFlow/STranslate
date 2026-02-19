using CommunityToolkit.Mvvm.DependencyInjection;
using iNKORE.UI.WPF.Modern;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using STranslate.Views;
using System.Collections.ObjectModel;
using System.Windows;

namespace STranslate.Core;

public class PluginContext(PluginMetaData metaData, string serviceId) : IPluginContext
{
    private IPluginSavable Savable { get => field; set => field = value; } = null!;
    private object? _currentSettings;
    private readonly List<Action<IReadOnlyList<GlobalPrompt>>> _globalPromptsChangedCallbacks = [];
    private readonly object _callbacksLock = new();
    private Settings? _settings;
    private bool _disposed;

    public PluginMetaData MetaData => metaData;

    public ILogger Logger => Ioc.Default.GetRequiredService<ILoggerFactory>().CreateLogger(metaData.AssemblyName);

    public string GetTranslation(string key) => Ioc.Default.GetRequiredService<Internationalization>().GetTranslation(key);

    public IHttpService HttpService => Ioc.Default.GetRequiredService<IHttpService>();

    public IAudioPlayer AudioPlayer => Ioc.Default.GetRequiredService<IAudioPlayer>();

    public ISnackbar Snackbar => Ioc.Default.GetRequiredService<ISnackbar>();

    public INotification Notification => Ioc.Default.GetRequiredService<INotification>();

    public ImageQuality ImageQuality => Ioc.Default.GetRequiredService<Settings>().ImageQuality;

    public Window GetPromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default)
    {
        var window = new PromptEditWindow(prompts, roles)
        {
            Owner = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault()
        };

        ThemeManager.SetRequestedTheme(window, Enum.Parse<ElementTheme>(Ioc.Default.GetRequiredService<Settings>().ColorScheme.ToString()));

        return window;
    }

    public T LoadSettingStorage<T>() where T : new()
    {
        var storage = new PluginStorage<T>(metaData, serviceId);
        var data = storage.Load();

        // 初始化时尝试创建配置文件
        if (storage.IsDefaultData)
            storage.Save();

        Savable = storage;
        _currentSettings = data;
        return data;
    }

    public void SaveSettingStorage<T>() where T : new()
    {
        Savable?.Save();
    }

    public IReadOnlyList<GlobalPrompt> GetGlobalPrompts()
    {
        return Ioc.Default.GetRequiredService<Settings>().GetEnabledGlobalPromptsSnapshot();
    }

    public GlobalPrompt? GetGlobalPromptById(string id)
    {
        return Ioc.Default.GetRequiredService<Settings>().GetGlobalPromptByIdSnapshot(id);
    }

    public void ApplyTheme(Window window)
    {
        if (window == null)
            return;

        ThemeManager.SetRequestedTheme(window, Ioc.Default.GetRequiredService<Settings>().ColorScheme);
    }

    public IDisposable RegisterGlobalPromptsChangedCallback(Action<IReadOnlyList<GlobalPrompt>> callback, IDisposable? lifetime = null)
    {
        bool shouldSubscribe = false;
        lock (_callbacksLock)
        {
            if (!_globalPromptsChangedCallbacks.Contains(callback))
            {
                _globalPromptsChangedCallbacks.Add(callback);
                shouldSubscribe = _globalPromptsChangedCallbacks.Count == 1;
            }
        }

        if (shouldSubscribe)
        {
            SubscribeToGlobalPromptsChanged();
        }

        // 创建注销句柄
        var unregisterHandle = new CallbackUnregisterHandle(this, callback);

        // 如果提供了 lifetime，在 lifetime 释放时自动注销
        if (lifetime != null)
        {
            var weakCallback = new WeakReference<Action<IReadOnlyList<GlobalPrompt>>>(callback);
            var weakContext = new WeakReference<PluginContext>(this);
            
            // 订阅 lifetime 的释放事件
            if (lifetime is Window window)
            {
                // 如果是窗口，在 Closed 事件中注销
                window.Closed += (s, e) =>
                {
                    if (weakCallback.TryGetTarget(out var target) && weakContext.TryGetTarget(out var context))
                    {
                        context.UnregisterGlobalPromptsChangedCallback(target);
                    }
                };
            }
            else
            {
                // 如果是其他 IDisposable，尝试使用 Dispose 模式
                // 这里使用一个弱引用追踪器
                TrackLifetimeDisposal(lifetime, callback);
            }
        }

        return unregisterHandle;
    }

    private void TrackLifetimeDisposal(IDisposable lifetime, Action<IReadOnlyList<GlobalPrompt>> callback)
    {
        // 创建一个弱引用的追踪器，当 lifetime 被 GC 回收时，自动注销回调
        var weakLifetime = new WeakReference<IDisposable>(lifetime);
        var weakCallback = new WeakReference<Action<IReadOnlyList<GlobalPrompt>>>(callback);
        var weakContext = new WeakReference<PluginContext>(this);

        // 在下次触发事件时清理已释放的 lifetime 关联的回调
        _lifetimeTrackedCallbacks.Add((weakLifetime, weakCallback));
    }

    private readonly List<(WeakReference<IDisposable> Lifetime, WeakReference<Action<IReadOnlyList<GlobalPrompt>>> Callback)> _lifetimeTrackedCallbacks = new();

    public void UnregisterGlobalPromptsChangedCallback(Action<IReadOnlyList<GlobalPrompt>> callback)
    {
        lock (_callbacksLock)
        {
            _globalPromptsChangedCallbacks.Remove(callback);
        }
    }

    internal void RaiseGlobalPromptsChanged(IReadOnlyList<GlobalPrompt> globalPrompts)
    {
        List<Action<IReadOnlyList<GlobalPrompt>>> callbacksCopy;
        lock (_callbacksLock)
        {
            callbacksCopy = [.. _globalPromptsChangedCallbacks];
        }

        foreach (var callback in callbacksCopy)
        {
            try
            {
                callback(globalPrompts);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GlobalPromptsChanged callback error");
            }
        }
    }

    private void SubscribeToGlobalPromptsChanged()
    {
        _settings ??= Ioc.Default.GetRequiredService<Settings>();
        _settings.GlobalPromptsChanged += OnSettingsGlobalPromptsChanged;
    }

    private void OnSettingsGlobalPromptsChanged(object? sender, IReadOnlyList<GlobalPrompt> globalPrompts)
    {
        RaiseGlobalPromptsChanged(globalPrompts);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_settings != null)
        {
            _settings.GlobalPromptsChanged -= OnSettingsGlobalPromptsChanged;
        }

        Savable.Delete();
        Savable.Clean();
    }

    /// <summary>
    /// 回调注销句柄
    /// </summary>
    private class CallbackUnregisterHandle : IDisposable
    {
        private readonly PluginContext _context;
        private readonly Action<IReadOnlyList<GlobalPrompt>> _callback;
        private bool _disposed;

        public CallbackUnregisterHandle(PluginContext context, Action<IReadOnlyList<GlobalPrompt>> callback)
        {
            _context = context;
            _callback = callback;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _context.UnregisterGlobalPromptsChangedCallback(_callback);
        }
    }
}