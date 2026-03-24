using System.Windows;
using System.Windows.Controls;
using STranslate.Services;

namespace STranslate.Controls;

public class MarkdownViewer : Control
{
    public static readonly DependencyProperty MarkdownTextProperty =
        DependencyProperty.Register(nameof(MarkdownText), typeof(string),
            typeof(MarkdownViewer), new PropertyMetadata(string.Empty, OnTextChanged));

    public string MarkdownText
    {
        get => (string)GetValue(MarkdownTextProperty);
        set => SetValue(MarkdownTextProperty, value);
    }

    public static readonly DependencyProperty EnableMarkdownProperty =
        DependencyProperty.Register(nameof(EnableMarkdown), typeof(bool),
            typeof(MarkdownViewer), new PropertyMetadata(false, OnTextChanged));

    public bool EnableMarkdown
    {
        get => (bool)GetValue(EnableMarkdownProperty);
        set => SetValue(EnableMarkdownProperty, value);
    }

    private TextBox? _textBox;

    static MarkdownViewer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer),
            new FrameworkPropertyMetadata(typeof(MarkdownViewer)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _textBox = GetTemplateChild("PART_Content") as TextBox;
        UpdateDisplay();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MarkdownViewer)d).UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_textBox == null) return;

        var text = MarkdownText ?? "";

        if (string.IsNullOrEmpty(text))
        {
            _textBox.Text = "";
            return;
        }

        _textBox.Text = text;
    }
}
