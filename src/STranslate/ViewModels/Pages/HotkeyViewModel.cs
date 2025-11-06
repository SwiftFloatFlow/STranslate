using STranslate.Core;
using STranslate.Plugin;

namespace STranslate.ViewModels.Pages;

public partial class HotkeyViewModel(HotkeySettings settings, IInternationalization i18n) : SearchViewModelBase(i18n, "Hotkey_")
{
    public HotkeySettings HotkeySettings { get; } = settings;
}