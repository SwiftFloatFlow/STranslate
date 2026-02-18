using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.Core;
using STranslate.ViewModels.Pages;
using System.Windows;

namespace STranslate.Views;

public partial class GlobalPromptEditWindow
{
    public GlobalPromptEditWindow()
    {
        InitializeComponent();

        // 获取依赖
        var settings = Ioc.Default.GetRequiredService<Settings>();
        var serviceManager = Ioc.Default.GetRequiredService<ServiceManager>();
        
        // 创建 ViewModel
        DataContext = new GlobalPromptViewModel(settings, serviceManager);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
