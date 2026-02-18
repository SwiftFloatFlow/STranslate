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
    /// 全局提示词列表
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<GlobalPrompt> GlobalPrompts { get; set; } = [];

    /// <summary>
    /// 选中的提示词
    /// </summary>
    [ObservableProperty]
    public partial GlobalPrompt? SelectedPrompt { get; set; }

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
    public IEnumerable<GlobalPrompt> FilteredPrompts => 
        string.IsNullOrWhiteSpace(SearchText) 
            ? GlobalPrompts 
            : GlobalPrompts.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public GlobalPromptViewModel(Settings settings)
    {
        _settings = settings;

        // 初始化列表
        RefreshGlobalPrompts();

        // 监听设置变化
        _settings.GlobalPrompts.CollectionChanged += (s, e) => RefreshGlobalPrompts();
    }

    /// <summary>
    /// 刷新全局提示词列表
    /// </summary>
    private void RefreshGlobalPrompts()
    {
        GlobalPrompts.Clear();
        foreach (var prompt in _settings.GlobalPrompts)
        {
            GlobalPrompts.Add(prompt);
        }
    }

    /// <summary>
    /// 添加新的全局提示词
    /// </summary>
    [RelayCommand]
    private void AddGlobalPrompt()
    {
        var newPrompt = GlobalPrompt.CreateDefault(
            "新全局提示词", 
            "你是一个专业的翻译助手", 
            "请翻译以下内容："
        );
        
        _settings.GlobalPrompts.Add(newPrompt);
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
    }

    /// <summary>
    /// 删除选中的全局提示词（直接删除，无需确认）
    /// </summary>
    [RelayCommand]
    private void DeleteGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        _settings.GlobalPrompts.Remove(SelectedPrompt);
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
    }

    /// <summary>
    /// 克隆选中的全局提示词
    /// </summary>
    [RelayCommand]
    private void CloneGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        var cloned = SelectedPrompt.Clone();
        _settings.GlobalPrompts.Add(cloned);
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
    }

    /// <summary>
    /// 上移
    /// </summary>
    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedPrompt == null) return;

        var index = _settings.GlobalPrompts.IndexOf(SelectedPrompt);
        if (index > 0)
        {
            _settings.GlobalPrompts.Move(index, index - 1);
            _settings.Save();
            _settings.RaiseGlobalPromptsChanged();
        }
    }

    /// <summary>
    /// 下移
    /// </summary>
    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedPrompt == null) return;

        var index = _settings.GlobalPrompts.IndexOf(SelectedPrompt);
        if (index < _settings.GlobalPrompts.Count - 1)
        {
            _settings.GlobalPrompts.Move(index, index + 1);
            _settings.Save();
            _settings.RaiseGlobalPromptsChanged();
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
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
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
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
    }

    /// <summary>
    /// 保存并关闭窗口
    /// </summary>
    [RelayCommand]
    private void Save(Window? window)
    {
        _settings.Save();
        _settings.RaiseGlobalPromptsChanged();
        window?.Close();
    }

    /// <summary>
    /// 取消并关闭窗口
    /// </summary>
    [RelayCommand]
    private void Cancel(Window? window)
    {
        window?.Close();
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredPrompts));
    }
}
