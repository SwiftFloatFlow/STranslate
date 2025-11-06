using CommunityToolkit.Mvvm.ComponentModel;

namespace STranslate.Plugin.Ocr.Paddle.ViewModel;

public partial class SettingsViewModel(IPluginContext context, Settings settings) : ObservableObject
{
    [ObservableProperty] public partial string ModelsDirectory { get; set; } = settings.ModelsDirectory;

    partial void OnModelsDirectoryChanged(string value)
    {
        settings.ModelsDirectory = value;
        context.SaveSettingStorage<Settings>();
    }
}