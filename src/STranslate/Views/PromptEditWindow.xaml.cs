using STranslate.Plugin;
using STranslate.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace STranslate.Views;

public partial class PromptEditWindow
{
    /// <summary>
    /// 指示是否为有效保存（内容有变化）
    /// </summary>
    public bool HasValidSave { get; private set; }

    public PromptEditWindow(ObservableCollection<Prompt> prompts, List<string>? roles = default, bool isMutualExclusion = true)
    {
        InitializeComponent();

        var viewModel = new PromptEditViewModel(prompts, roles, isMutualExclusion);
        viewModel.SaveRequested += OnSaveRequested;
        DataContext = viewModel;

        Closing += OnClosing;
    }

    private void OnSaveRequested(bool hasChanges) => HasValidSave = hasChanges;

    private void OnClosing(object? s, CancelEventArgs e)
    {
        if (DataContext is PromptEditViewModel viewModel)
        {
            viewModel.SaveRequested -= OnSaveRequested;
            viewModel.Dispose();
        }

        // 取消订阅 Closing 事件，防止窗口实例被缓存时的内存泄漏
        Closing -= OnClosing;
    }
}
