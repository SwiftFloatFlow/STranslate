using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.Instances;

public partial class OcrInstance : ServiceInstanceBase
{
    protected override ServiceType ServiceType => ServiceType.OCR;

    public OcrInstance(
        PluginManager pluginManager,
        ServiceManager serviceManager,
        PluginInstance pluginInstance,
        ServiceSettings serviceSettings,
        IInternationalization i18n
    ) : base(pluginManager, serviceManager, pluginInstance, serviceSettings, i18n)
    {
        LoadPlugins<IOcrPlugin>();
        LoadServices<IOcrPlugin>();
    }
}
