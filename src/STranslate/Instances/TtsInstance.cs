using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.Instances;

public partial class TtsInstance : ServiceInstanceBase
{
    protected override ServiceType ServiceType => ServiceType.TTS;

    public TtsInstance(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginInstance pluginInstance,
        ServiceSettings serviceSettings,
        IInternationalization i18n
    ) : base(pluginManager, serviceManager, pluginInstance, serviceSettings, i18n)
    {
        LoadPlugins<ITtsPlugin>();
        LoadServices<ITtsPlugin>();
    }
}
