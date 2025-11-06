using CommunityToolkit.Mvvm.DependencyInjection;
using STranslate.ViewModels;
using System.ComponentModel;

namespace STranslate.Views;

public partial class OcrWindow
{
    private readonly OcrWindowViewModel _viewModel;

    public OcrWindow()
    {
        _viewModel = Ioc.Default.GetRequiredService<OcrWindowViewModel>();
        DataContext = _viewModel;

        InitializeComponent();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _viewModel.CancelOperations();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
