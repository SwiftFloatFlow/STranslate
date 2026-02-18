using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace STranslate.Plugin;

/// <summary>
/// 全局提示词定义
/// </summary>
public partial class GlobalPrompt : ObservableObject
{
    /// <summary>
    /// 唯一标识符（GUID格式）
    /// </summary>
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = "新全局提示词";

    /// <summary>
    /// 是否启用（启用后才会通过接口暴露给插件）
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 提示词内容列表
    /// </summary>
    public ObservableCollection<PromptItem> Items { get; set; } = [];

    /// <summary>
    /// 转换为普通 Prompt（供插件使用）
    /// </summary>
    /// <param name="isEnabled">是否启用</param>
    /// <returns>转换后的 Prompt</returns>
    public Prompt ToPrompt(bool isEnabled = false)
    {
        var prompt = new Prompt
        {
            Name = $"[Global:{Id}] {Name}",
            IsEnabled = isEnabled
        };
        
        foreach (var item in Items)
        {
            prompt.Items.Add(item.Clone());
        }
        
        prompt.Tag = $"Global:{Id}";
        
        return prompt;
    }

    /// <summary>
    /// 克隆全局提示词（用于编辑窗口复制，生成新ID）
    /// </summary>
    public GlobalPrompt Clone()
    {
        return new GlobalPrompt
        {
            Id = Guid.NewGuid().ToString("N"),  // 生成新ID，避免重复
            Name = Name + " (副本)",
            IsEnabled = IsEnabled,
            Items = new ObservableCollection<PromptItem>(
                Items.Select(i => i.Clone())
            )
        };
    }

    /// <summary>
    /// 创建只读副本（用于接口返回，保护原始数据）
    /// </summary>
    internal GlobalPrompt CloneForRead()
    {
        return new GlobalPrompt
        {
            Id = Id,
            Name = Name,
            IsEnabled = IsEnabled,
            Items = new ObservableCollection<PromptItem>(
                Items.Select(i => i.Clone())
            )
        };
    }

    /// <summary>
    /// 创建默认的全局提示词
    /// </summary>
    public static GlobalPrompt CreateDefault(string name, string systemContent, string userContent)
    {
        return new GlobalPrompt
        {
            Name = name,
            Items =
            [
                new PromptItem("system", systemContent),
                new PromptItem("user", userContent)
            ]
        };
    }
}
