using STranslate.Plugin;
using STranslate.ViewModels;
using System.Collections.ObjectModel;

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
        viewModel.SaveRequested += (hasChanges) =>
        {
            HasValidSave = hasChanges;
        };
        DataContext = viewModel;

        Closing += (s, e) => viewModel.Dispose();
    }
}
