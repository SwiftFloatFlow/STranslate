using CommunityToolkit.Mvvm.Input;
using STranslate.Services;
using STranslate.Plugin;
using STranslate.Views;
using System.Windows;

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
        var window = new GlobalPromptEditWindow();
        window.ShowDialog();
    }
}