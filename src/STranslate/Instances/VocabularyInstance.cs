using CommunityToolkit.Mvvm.ComponentModel;
using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.Instances;

public partial class VocabularyInstance : ServiceInstanceBase
{
    protected override ServiceType ServiceType => ServiceType.Vocabulary;
    [ObservableProperty] public partial bool HasActivedVocabulary { get; set; } = false;

    public VocabularyInstance(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginInstance pluginInstance,
        ServiceSettings serviceSettings,
        IInternationalization i18n
    ) : base(pluginManager, serviceManager, pluginInstance, serviceSettings, i18n)
    {
        LoadPlugins<IVocabularyPlugin>();
        LoadServices<IVocabularyPlugin>();

        HasActivedVocabulary = Services?.Any(s => s.IsEnabled) ?? false;
        OnSvcPropertyChanged += OnSvcPropertyChangedHandler;
    }

    public override void Dispose()
    {
        OnSvcPropertyChanged -= OnSvcPropertyChangedHandler;
        base.Dispose();
    }

    private void OnSvcPropertyChangedHandler() => HasActivedVocabulary = Services.Any(s => s.IsEnabled);
}
