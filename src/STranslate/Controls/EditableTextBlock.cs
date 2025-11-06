using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace STranslate.Controls;

public class EditableTextBlock : Control
{
    static EditableTextBlock()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(typeof(EditableTextBlock)));
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public static readonly DependencyProperty IsEditingProperty =
        DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableTextBlock),
            new FrameworkPropertyMetadata(false));

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_TextBlock") is TextBlock tb)
        {
            tb.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    IsEditing = true;

                    // 延迟到UI渲染后再Focus+SelectAll
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (GetTemplateChild("PART_TextBox") is TextBox box)
                        {
                            box.Focus();
                            box.SelectAll();
                        }
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            };
        }

        if (GetTemplateChild("PART_TextBox") is TextBox box)
        {
            box.LostFocus += (s, e) => IsEditing = false;
            box.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    IsEditing = false;
                }
                else if (e.Key == Key.Escape)
                {
                    // 取消编辑时回退原值
                    box.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                    IsEditing = false;
                }
            };
        }
    }
}