using CommunityToolkit.Mvvm.ComponentModel;
using STranslate.Models;

namespace STranslate.ViewModels;

/// <summary>
/// 可选择的全局提示词包装类（用于UI绑定）
/// </summary>
public partial class SelectableGlobalPrompt : ObservableObject
{
    /// <summary>
    /// 全局提示词
    /// </summary>
    public GlobalPrompt GlobalPrompt { get; }

    /// <summary>
    /// 是否被选中
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// 引用计数（显示有多少服务在使用此提示词）
    /// </summary>
    [ObservableProperty]
    public partial int ReferenceCount { get; set; }

    public SelectableGlobalPrompt(GlobalPrompt globalPrompt)
    {
        GlobalPrompt = globalPrompt;
    }

    /// <summary>
    /// ID（透传）
    /// </summary>
    public string Id => GlobalPrompt.Id;

    /// <summary>
    /// 名称（透传）
    /// </summary>
    public string Name => GlobalPrompt.Name;

    /// <summary>
    /// 提示词内容数量
    /// </summary>
    public int ItemCount => GlobalPrompt.Items.Count;

    /// <summary>
    /// 显示文本（名称 + 引用数）
    /// </summary>
    public string DisplayText => ReferenceCount > 0 
        ? $"{Name} ({ReferenceCount})" 
        : Name;
}
