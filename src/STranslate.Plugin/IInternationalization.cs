namespace STranslate.Plugin;

/// <summary>
/// 国际化接口
/// </summary>
public interface IInternationalization
{
    /// <summary>
    /// 初始化语言
    /// </summary>
    /// <param name="languageCode"></param>
    void InitializeLanguage(string languageCode);

    /// <summary>
    /// 改变语言
    /// </summary>
    /// <param name="languageCode"></param>
    void ChangeLanguage(string languageCode);

    /// <summary>
    /// 加载已安装插件的语言
    /// </summary>
    /// <param name="pluginDirectory"></param>
    void LoadInstalledPluginLanguages(string pluginDirectory);

    /// <summary>
    /// 加载可用语言
    /// </summary>
    /// <returns></returns>
    List<I18nPair> LoadAvailableLanguages();

    /// <summary>
    /// 获取翻译
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    string GetTranslation(string key);

    /// <summary>
    /// 语言改变事件
    /// </summary>
    event Action? OnLanguageChanged;
}
