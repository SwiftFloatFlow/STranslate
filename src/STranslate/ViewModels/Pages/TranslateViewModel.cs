using CommunityToolkit.Mvvm.Input;
using STranslate.Instances;
using STranslate.Plugin;

namespace STranslate.ViewModels.Pages;

public partial class TranslateViewModel(TranslateInstance instance) : ServiceViewModelBase<TranslateInstance>(instance)
{
    // 翻译服务特有的功能
    [RelayCommand]
    private void ActiveReplace(Service svc) => Instance.ActiveReplace(svc);

    [RelayCommand]
    private void DeactiveReplace() => Instance.DeactiveReplace();

    [RelayCommand]
    private void ActiveImTran(Service svc) => Instance.ActiveImTran(svc);

    [RelayCommand]
    private void DeactiveImTran() => Instance.DeactiveImTran();
}