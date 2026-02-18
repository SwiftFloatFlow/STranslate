using STranslate.ViewModels.Pages;

namespace STranslate.Views.Pages;

/// <summary>
/// GlobalPromptPage.xaml 的交互逻辑
/// </summary>
public partial class GlobalPromptPage
{
    public GlobalPromptPage(GlobalPromptViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = ViewModel;

        InitializeComponent();
    }

    public GlobalPromptViewModel ViewModel { get; }
}
