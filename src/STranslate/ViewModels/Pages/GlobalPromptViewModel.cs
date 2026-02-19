using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Core;
using STranslate.Plugin;
using System.Collections.ObjectModel;
using System.Windows;

namespace STranslate.ViewModels.Pages;

/// <summary>
/// 全局提示词页面 ViewModel
/// </summary>
public partial class GlobalPromptViewModel : ObservableObject
{
    private readonly Settings _settings;

    /// <summary>
    /// 全局提示词列表（编辑中的克隆列表）
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<Prompt> GlobalPrompts { get; set; } = [];

    /// <summary>
    /// 选中的提示词
    /// </summary>
    [ObservableProperty]
    public partial Prompt? SelectedPrompt { get; set; }

    /// <summary>
    /// 选中的提示项
    /// </summary>
    [ObservableProperty]
    public partial PromptItem? SelectedPromptItem { get; set; }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    /// <summary>
    /// 过滤后的提示词列表
    /// </summary>
    public IEnumerable<Prompt> FilteredPrompts => 
        string.IsNullOrWhiteSpace(SearchText) 
            ? GlobalPrompts 
            : GlobalPrompts.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public GlobalPromptViewModel(Settings settings)
    {
        _settings = settings;

        // 克隆全局提示词列表进行编辑
        // 这样 Cancel 可以真正撤销修改
        foreach (var prompt in _settings.GlobalPrompts)
        {
            GlobalPrompts.Add(prompt.Clone());
        }
    }

    /// <summary>
    /// 添加新的全局提示词
    /// </summary>
    [RelayCommand]
    private void AddGlobalPrompt()
    {
        var newPrompt = new Prompt
        {
            Name = GenerateUniqueName("新全局提示词"),
            IsEnabled = true,
            Items = new ObservableCollection<PromptItem>
            {
                new PromptItem("system", "你是一个专业的翻译助手"),
                new PromptItem("user", "请翻译以下内容：")
            }
        };
        
        GlobalPrompts.Add(newPrompt);
        SelectedPrompt = newPrompt;
    }

    /// <summary>
    /// 删除选中的全局提示词
    /// </summary>
    [RelayCommand]
    private void DeleteGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        GlobalPrompts.Remove(SelectedPrompt);
        SelectedPrompt = null;
        SelectedPromptItem = null;
    }

    /// <summary>
    /// 克隆选中的全局提示词
    /// </summary>
    [RelayCommand]
    private void CloneGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        var cloned = SelectedPrompt.Clone();
        cloned.Name = GenerateUniqueName(cloned.Name + " (副本)");
        GlobalPrompts.Add(cloned);
        SelectedPrompt = cloned;
    }

    /// <summary>
    /// 上移
    /// </summary>
    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedPrompt == null) return;

        var index = GlobalPrompts.IndexOf(SelectedPrompt);
        if (index > 0)
        {
            GlobalPrompts.Move(index, index - 1);
        }
    }

    /// <summary>
    /// 下移
    /// </summary>
    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedPrompt == null) return;

        var index = GlobalPrompts.IndexOf(SelectedPrompt);
        if (index < GlobalPrompts.Count - 1)
        {
            GlobalPrompts.Move(index, index + 1);
        }
    }

    /// <summary>
    /// 添加提示项
    /// </summary>
    [RelayCommand]
    private void AddPromptItem()
    {
        if (SelectedPrompt == null) return;

        var newItem = new PromptItem("system", "");
        SelectedPrompt.Items.Add(newItem);
        SelectedPromptItem = newItem;
    }

    /// <summary>
    /// 删除提示项
    /// </summary>
    [RelayCommand]
    private void RemovePromptItem()
    {
        if (SelectedPrompt == null || SelectedPromptItem == null) return;

        SelectedPrompt.Items.Remove(SelectedPromptItem);
        SelectedPromptItem = null;
    }

    /// <summary>
    /// 保存并关闭窗口
    /// </summary>
    [RelayCommand]
    private void Save(Window? window)
    {
        try
        {
            // 将编辑后的列表写回 Settings
            _settings.GlobalPrompts.Clear();
            foreach (var prompt in GlobalPrompts)
            {
                _settings.GlobalPrompts.Add(prompt);
            }
            
            _settings.Save();
            _settings.RaiseGlobalPromptsChanged();
            window?.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 取消并关闭窗口
    /// </summary>
    [RelayCommand]
    private void Cancel(Window? window)
    {
        // 直接关闭窗口，不保存修改
        // 因为所有编辑都在 GlobalPrompts 克隆列表上进行
        // Settings.GlobalPrompts 未被修改
        window?.Close();
    }

    /// <summary>
    /// 生成唯一的提示词名称
    /// </summary>
    private string GenerateUniqueName(string baseName)
    {
        if (!GlobalPrompts.Any(p => p.Name == baseName))
        {
            return baseName;
        }

        // 如果存在重名，添加序号
        int counter = 1;
        string newName;
        do
        {
            newName = $"{baseName} ({counter})";
            counter++;
        } while (GlobalPrompts.Any(p => p.Name == newName));

        return newName;
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredPrompts));
    }
}
