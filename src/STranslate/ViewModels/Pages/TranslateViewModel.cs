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

    [RelayCommand]
    private void ReorderEnabledServices()
    {
        var services = Service.Services;
        if (services.Count <= 1)
            return;

        var targetOrder = new List<Service>(services.Count);
        foreach (var svc in services)
        {
            if (svc.IsEnabled)
                targetOrder.Add(svc);
        }

        foreach (var svc in services)
        {
            if (!svc.IsEnabled)
                targetOrder.Add(svc);
        }

        // 按目标顺序执行 Move，复用现有顺序持久化逻辑，且保持组内相对顺序稳定。
        for (var targetIndex = 0; targetIndex < targetOrder.Count; targetIndex++)
        {
            var service = targetOrder[targetIndex];
            var currentIndex = services.IndexOf(service);
            if (currentIndex >= 0 && currentIndex != targetIndex)
                services.Move(currentIndex, targetIndex);
        }
    }
}
