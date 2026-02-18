using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STranslate.Core;
using STranslate.Models;
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
    private readonly ServiceManager _serviceManager;

    /// <summary>
    /// 全局提示词列表（可选择）
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<SelectableGlobalPrompt> GlobalPrompts { get; set; } = [];

    /// <summary>
    /// 选中的提示词
    /// </summary>
    [ObservableProperty]
    public partial SelectableGlobalPrompt? SelectedPrompt { get; set; }

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
    public IEnumerable<SelectableGlobalPrompt> FilteredPrompts => 
        string.IsNullOrWhiteSpace(SearchText) 
            ? GlobalPrompts 
            : GlobalPrompts.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public GlobalPromptViewModel(Settings settings, ServiceManager serviceManager)
    {
        _settings = settings;
        _serviceManager = serviceManager;

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
            var selectable = new SelectableGlobalPrompt(prompt);
            selectable.ReferenceCount = CalculateReferenceCount(prompt.Id);
            GlobalPrompts.Add(selectable);
        }
    }

    /// <summary>
    /// 计算引用计数
    /// </summary>
    private int CalculateReferenceCount(string globalPromptId)
    {
        return _serviceManager.Services
            .Where(s => s.Options?.ReferencedGlobalPromptIds.Contains(globalPromptId) == true)
            .Count();
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
    }

    /// <summary>
    /// 删除选中的全局提示词
    /// </summary>
    [RelayCommand]
    private void DeleteGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        var result = MessageBox.Show(
            $"确定要删除全局提示词 \"{SelectedPrompt.Name}\" 吗？\n\n注意：这会导致所有引用此提示词的服务失去该提示词。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var promptToRemove = _settings.GlobalPrompts.FirstOrDefault(p => p.Id == SelectedPrompt.Id);
            if (promptToRemove != null)
            {
                _settings.GlobalPrompts.Remove(promptToRemove);
                _settings.Save();
            }
        }
    }

    /// <summary>
    /// 克隆选中的全局提示词
    /// </summary>
    [RelayCommand]
    private void CloneGlobalPrompt()
    {
        if (SelectedPrompt == null) return;

        var cloned = SelectedPrompt.GlobalPrompt.Clone();
        _settings.GlobalPrompts.Add(cloned);
        _settings.Save();
    }

    /// <summary>
    /// 上移
    /// </summary>
    [RelayCommand]
    private void MoveUp()
    {
        if (SelectedPrompt == null) return;

        var index = _settings.GlobalPrompts.IndexOf(SelectedPrompt.GlobalPrompt);
        if (index > 0)
        {
            _settings.GlobalPrompts.Move(index, index - 1);
            _settings.Save();
        }
    }

    /// <summary>
    /// 下移
    /// </summary>
    [RelayCommand]
    private void MoveDown()
    {
        if (SelectedPrompt == null) return;

        var index = _settings.GlobalPrompts.IndexOf(SelectedPrompt.GlobalPrompt);
        if (index < _settings.GlobalPrompts.Count - 1)
        {
            _settings.GlobalPrompts.Move(index, index + 1);
            _settings.Save();
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
        SelectedPrompt.GlobalPrompt.Items.Add(newItem);
        SelectedPromptItem = newItem;
        _settings.Save();
    }

    /// <summary>
    /// 删除提示项
    /// </summary>
    [RelayCommand]
    private void RemovePromptItem()
    {
        if (SelectedPrompt == null || SelectedPromptItem == null) return;

        SelectedPrompt.GlobalPrompt.Items.Remove(SelectedPromptItem);
        SelectedPromptItem = null;
        _settings.Save();
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    [RelayCommand]
    private void Close(Window? window)
    {
        window?.Close();
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredPrompts));
    }
}
