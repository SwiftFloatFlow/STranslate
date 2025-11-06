using CommunityToolkit.Mvvm.ComponentModel;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Plugin;
using System.Windows;

namespace STranslate.Instances;

public partial class TranslateInstance : ServiceInstanceBase
{
    private readonly ServiceSettings _serviceSettings;

    [ObservableProperty] public partial Service? ReplaceService { get; set; }
    [ObservableProperty] public partial Service? ImageTranslateService { get; set; }

    protected override ServiceType ServiceType => ServiceType.Translation;

    public TranslateInstance(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginInstance pluginInstance,
        ServiceSettings serviceSettings,
        IInternationalization i18n
    ) : base(pluginManager, serviceManager, pluginInstance, serviceSettings, i18n)
    {
        _serviceSettings = serviceSettings;

        LoadPlugins<ITranslatePlugin>();
        LoadPlugins<IDictionaryPlugin>();
        LoadServices<ITranslatePlugin, IDictionaryPlugin>();
        InitialOtherService();
    }

    private void InitialOtherService()
    {
        ReplaceService = Services.FirstOrDefault(s => s.ServiceID == _serviceSettings.ReplaceSvcID);
        ImageTranslateService = Services.FirstOrDefault(s => s.ServiceID == _serviceSettings.ImageTranslateSvcID);
    }

    public override async Task<bool> DeleteAsync(Service service)
    {
        var result = await base.DeleteAsync(service);
        if (result && service == ReplaceService)
        {
            ReplaceService = null;
            _serviceSettings.ReplaceSvcID = string.Empty;
            _serviceSettings.Save();
        }
        else if (result && service == ImageTranslateService)
        {
            ImageTranslateService = null;
            _serviceSettings.ImageTranslateSvcID = string.Empty;
            _serviceSettings.Save();
        }

        return result;
    }

    internal void ActiveImTran(Service svc)
    {
        if (svc.Plugin is IDictionaryPlugin)
        {
            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("词典服务不支持替换功能。", Constant.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ImageTranslateService = svc;

        _serviceSettings.ImageTranslateSvcID = ImageTranslateService?.ServiceID ?? string.Empty;
        _serviceSettings.Save();
    }

    internal void DeactiveImTran()
    {
        ImageTranslateService = null;
        _serviceSettings.ImageTranslateSvcID = string.Empty;
        _serviceSettings.Save();
    }

    internal void ActiveReplace(Service svc)
    {
        if (svc.Plugin is IDictionaryPlugin)
        {
            iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("词典服务不支持替换功能。", Constant.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ReplaceService = svc;

        _serviceSettings.ReplaceSvcID = ReplaceService?.ServiceID ?? string.Empty;
        _serviceSettings.Save();
    }

    internal void DeactiveReplace()
    {
        ReplaceService = null;
        _serviceSettings.ReplaceSvcID = string.Empty;
        _serviceSettings.Save();
    }
}
