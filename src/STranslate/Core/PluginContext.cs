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
        var window = new PromptEditWindow(prompts, roles, isMutualExclusion: true)
        {
            Owner = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault()
        };

        ThemeManager.SetRequestedTheme(window, Enum.Parse<ElementTheme>(Ioc.Default.GetRequiredService<Settings>().ColorScheme.ToString()));

        return window;
    }

    public IReadOnlyList<Prompt> GetGlobalPrompts()
    {
        var settings = Ioc.Default.GetRequiredService<Settings>();
        return settings.GlobalPrompts
            .Where(p => p.IsEnabled)
            .Select(p =>
            {
                var clone = p.Clone();
                clone.IsEnabled = false;
                return clone;
            })
            .ToList().AsReadOnly();
    }

    public Window GetGlobalPromptEditWindow()
    {
        var settings = Ioc.Default.GetRequiredService<Settings>();
        var window = new PromptEditWindow(settings.GlobalPrompts, roles: null, isMutualExclusion: false)
        {
            Owner = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault()
        };

        ThemeManager.SetRequestedTheme(window, Enum.Parse<ElementTheme>(settings.ColorScheme.ToString()));

        return window;
    }

    public IDisposable RegisterGlobalPromptsChangedCallback(Action<IReadOnlyList<Prompt>> callback, int delayMs = 100)
    {
        var settings = Ioc.Default.GetRequiredService<Settings>();

        settings.GlobalPromptsChanged += OnGlobalPromptsChanged;

        return new CallbackRegistration(() =>
        {
            Task.Delay(delayMs).ContinueWith(_ =>
            {
                settings.GlobalPromptsChanged -= OnGlobalPromptsChanged;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        });

        void OnGlobalPromptsChanged(IReadOnlyList<Prompt> prompts)
        {
            var clonedPrompts = prompts
                .Select(p =>
                {
                    var clone = p.Clone();
                    clone.IsEnabled = false;
                    return clone;
                })
                .ToList().AsReadOnly();
            callback(clonedPrompts);
        }
    }

    public T LoadSettingStorage<T>() where T : new()
    {
        var storage = new PluginStorage<T>(metaData, serviceId);
        var data = storage.Load();

        // 初始化时尝试创建配置文件
        if (storage.IsDefaultData)
            storage.Save();

        Savable = storage;
        return data;
    }

    public void SaveSettingStorage<T>() where T : new() => Savable?.Save();

    public void ApplyTheme(Window window)
    {
        if (window == null)
            return;

        ThemeManager.SetRequestedTheme(window, Ioc.Default.GetRequiredService<Settings>().ColorScheme);
    }

    public void Dispose()
    {
        Savable.Delete();
        Savable.Clean();
    }
}

/// <summary>
/// 回调注册的可释放对象，用于注销全局提示词变更回调
/// </summary>
file class CallbackRegistration(Action unregister) : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        unregister?.Invoke();
    }
}