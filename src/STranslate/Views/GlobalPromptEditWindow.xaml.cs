using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.ViewModels.Pages;
using STranslate.Views.Pages;

namespace STranslate.Views;

/// <summary>
/// GlobalPromptEditWindow.xaml 的交互逻辑
/// </summary>
public partial class GlobalPromptEditWindow
{
    public GlobalPromptEditWindow()
    {
        InitializeComponent();

        // 从依赖注入获取ViewModel
        var viewModel = Ioc.Default.GetRequiredService<GlobalPromptViewModel>();
        var page = new GlobalPromptPage(viewModel);
        ContentFrame.Content = page;
    }
}
