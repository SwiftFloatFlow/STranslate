using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using STranslate.Core;
using STranslate.Services;
using STranslate.Plugin;
using STranslate.Views;

namespace STranslate.ViewModels.Pages;

public partial class TranslateViewModel(TranslateService service) : ServiceViewModelBase<TranslateService>(service)
{
    // 翻译服务特有的功能
    [RelayCommand]
    private void ActiveReplace(Service svc) => Service.ActiveReplace(svc);

    [RelayCommand]
    private void DeactiveReplace() => Service.DeactiveReplace();

    [RelayCommand]
    private void ActiveImTran(Service svc) => Service.ActiveImTran(svc);

    [RelayCommand]
    private void DeactiveImTran() => Service.DeactiveImTran();

    [RelayCommand]
    private void EditGlobalPrompts()
    {
        var settings = Ioc.Default.GetRequiredService<Settings>();
        var window = new PromptEditWindow(settings.GlobalPrompts, roles: null, isMutualExclusion: false)
        {
            Owner = System.Windows.Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault()
        };

        iNKORE.UI.WPF.Modern.ThemeManager.SetRequestedTheme(
            window,
            Enum.Parse<iNKORE.UI.WPF.Modern.ElementTheme>(settings.ColorScheme.ToString()));

        var result = window.ShowDialog();
        
        // 只有在有效保存（有变更）时才触发通知
        if (result == true && window.HasValidSave)
        {
            settings.NotifyGlobalPromptsChanged();
        }
    }
}