using HtmlAgilityPack;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace STranslate.Converters;

/// <summary>
/// HTML 到 FlowDocument 完整转换器
/// </summary>
public static class HtmlToFlowDocumentConverter
{
    public static FlowDocument Convert(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var flowDocument = new FlowDocument
        {
            FontSize = 14,
            FontFamily = new FontFamily("Segoe UI, Microsoft YaHei"),
            PagePadding = new Thickness(0),
            LineHeight = 1.6
        };

        if (doc.DocumentNode != null)
        {
            foreach (var node in doc.DocumentNode.ChildNodes)
            {
                var block = ConvertNodeToBlock(node);
                if (block != null)
                {
                    flowDocument.Blocks.Add(block);
                }
            }
        }

        return flowDocument;
    }

    private static Block? ConvertNodeToBlock(HtmlNode node)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = HtmlEntity.DeEntitize(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return new Paragraph(new Run(text));
                }
                return null;

            case HtmlNodeType.Element:
                return ConvertElementToBlock(node);

            default:
                return null;
        }
    }

    private static Block? ConvertElementToBlock(HtmlNode node)
    {
        var tagName = node.Name.ToLowerInvariant();

        return tagName switch
        {
            "p" or "div" => CreateParagraph(node),
            "h1" => CreateHeading(node, 24, FontWeights.Bold),
            "h2" => CreateHeading(node, 20, FontWeights.Bold),
            "h3" => CreateHeading(node, 18, FontWeights.Bold),
            "h4" => CreateHeading(node, 16, FontWeights.Bold),
            "h5" => CreateHeading(node, 14, FontWeights.Bold),
            "h6" => CreateHeading(node, 12, FontWeights.Bold),
            "blockquote" => CreateBlockquote(node),
            "ul" or "ol" => CreateList(node, tagName == "ol"),
            "pre" => CreateCodeBlock(node),
            "table" => CreateTable(node),
            "hr" => new Paragraph(new LineBreak()),
            "br" => new Paragraph(new LineBreak()),
            _ => CreateParagraph(node)
        };
    }

    private static Paragraph CreateParagraph(HtmlNode node)
    {
        var paragraph = new Paragraph();
        paragraph.Margin = new Thickness(0, 4, 0, 4);

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                paragraph.Inlines.Add(inline);
            }
        }

        return paragraph;
    }

    private static Inline? ConvertNodeToInline(HtmlNode node)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = HtmlEntity.DeEntitize(node.InnerText);
                return string.IsNullOrEmpty(text) ? null : new Run(text);

            case HtmlNodeType.Element:
                return ConvertElementToInline(node);

            default:
                return null;
        }
    }

    private static Inline? ConvertElementToInline(HtmlNode node)
    {
        var tagName = node.Name.ToLowerInvariant();

        switch (tagName)
        {
            case "strong" or "b":
                return CreateStyledInline(node, FontWeights.Bold);

            case "em" or "i":
                return CreateStyledInline(node, null, FontStyles.Italic);

            case "del":
                return CreateStyledInline(node, null, null, TextDecorations.Strikethrough);

            case "code":
                return CreateCodeInline(node);

            case "a":
                return CreateHyperlink(node);

            case "sup":
                return CreateSuperscript(node);

            case "sub":
                return CreateSubscript(node);

            case "br":
                return new LineBreak();

            case "span":
                var span = new Span();
                foreach (var child in node.ChildNodes)
                {
                    var inline = ConvertNodeToInline(child);
                    if (inline != null)
                    {
                        span.Inlines.Add(inline);
                    }
                }
                return span.Inlines.Count > 0 ? span : null;

            default:
                // 递归处理未知标签
                var container = new Span();
                foreach (var child in node.ChildNodes)
                {
                    var inline = ConvertNodeToInline(child);
                    if (inline != null)
                    {
                        container.Inlines.Add(inline);
                    }
                }
                return container.Inlines.Count > 0 ? container : null;
        }
    }

    private static Inline CreateStyledInline(HtmlNode node, FontWeight? weight = null, FontStyle? style = null, TextDecorationCollection? decoration = null)
    {
        var span = new Span();

        if (weight.HasValue)
            span.FontWeight = weight.Value;
        if (style.HasValue)
            span.FontStyle = style.Value;
        if (decoration != null)
            span.TextDecorations = decoration;

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                span.Inlines.Add(inline);
            }
        }

        return span;
    }

    private static Inline CreateCodeInline(HtmlNode node)
    {
        var span = new Span
        {
            FontFamily = new FontFamily("Consolas, Monaco, Courier New"),
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            Foreground = new SolidColorBrush(Color.FromRgb(200, 40, 40))
        };

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                span.Inlines.Add(inline);
            }
        }

        return span;
    }

    private static Inline CreateHyperlink(HtmlNode node)
    {
        var href = node.GetAttributeValue("href", "");

        var hyperlink = new Hyperlink
        {
            NavigateUri = new Uri(href, UriKind.RelativeOrAbsolute),
            Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
            TextDecorations = TextDecorations.Underline
        };

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                hyperlink.Inlines.Add(inline);
            }
        }

        hyperlink.RequestNavigate += (s, e) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.ToString(),
                    UseShellExecute = true
                });
            }
            catch { }
            e.Handled = true;
        };

        return hyperlink;
    }

    private static Inline CreateSuperscript(HtmlNode node)
    {
        var span = new Span
        {
            BaselineAlignment = BaselineAlignment.Superscript,
            FontSize = 10
        };

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                span.Inlines.Add(inline);
            }
        }

        return span;
    }

    private static Inline CreateSubscript(HtmlNode node)
    {
        var span = new Span
        {
            BaselineAlignment = BaselineAlignment.Subscript,
            FontSize = 10
        };

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                span.Inlines.Add(inline);
            }
        }

        return span;
    }

    private static Paragraph CreateHeading(HtmlNode node, double fontSize, FontWeight weight)
    {
        var paragraph = new Paragraph();
        paragraph.FontSize = fontSize;
        paragraph.FontWeight = weight;
        paragraph.Margin = new Thickness(0, 8, 0, 4);

        foreach (var child in node.ChildNodes)
        {
            var inline = ConvertNodeToInline(child);
            if (inline != null)
            {
                paragraph.Inlines.Add(inline);
            }
        }

        return paragraph;
    }

    private static Block CreateBlockquote(HtmlNode node)
    {
        var section = new Section();
        section.BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        section.BorderThickness = new Thickness(4, 0, 0, 0);
        section.Padding = new Thickness(12, 4, 4, 4);
        section.Margin = new Thickness(0, 4, 0, 4);
        section.Background = new SolidColorBrush(Color.FromRgb(250, 250, 250));

        foreach (var child in node.ChildNodes)
        {
            var block = ConvertNodeToBlock(child);
            if (block != null)
            {
                section.Blocks.Add(block);
            }
        }

        return section;
    }

    private static List CreateList(HtmlNode node, bool isOrdered)
    {
        var list = new List
        {
            Margin = new Thickness(20, 4, 0, 4)
        };

        if (isOrdered)
        {
            list.MarkerStyle = TextMarkerStyle.Decimal;
        }
        else
        {
            list.MarkerStyle = TextMarkerStyle.Disc;
        }

        foreach (var child in node.ChildNodes)
        {
            if (child.Name.ToLowerInvariant() == "li")
            {
                var listItem = new ListItem();

                foreach (var grandChild in child.ChildNodes)
                {
                    var block = ConvertNodeToBlock(grandChild);
                    if (block != null)
                    {
                        listItem.Blocks.Add(block);
                    }
                }

                list.ListItems.Add(listItem);
            }
        }

        return list;
    }

    /// <summary>
    /// 创建代码块（优化版：递归处理子节点）
    /// </summary>
    private static Block CreateCodeBlock(HtmlNode node)
    {
        var section = new Section();
        section.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        section.Padding = new Thickness(8);
        section.Margin = new Thickness(0, 4, 0, 4);

        // 递归处理 pre 内的所有子节点
        foreach (var child in node.ChildNodes)
        {
            var block = ConvertCodeBlockContent(child);
            if (block != null)
            {
                // 应用代码块样式
                if (block is Paragraph p)
                {
                    p.FontFamily = new FontFamily("Consolas, Monaco, Courier New");
                    p.FontSize = 12;
                    p.Margin = new Thickness(0, 2, 0, 2);
                }
                section.Blocks.Add(block);
            }
        }

        return section;
    }

    /// <summary>
    /// 转换代码块内容（支持代码块内的行内格式）
    /// </summary>
    private static Block? ConvertCodeBlockContent(HtmlNode node)
    {
        switch (node.NodeType)
        {
            case HtmlNodeType.Text:
                var text = HtmlEntity.DeEntitize(node.InnerText);
                if (!string.IsNullOrEmpty(text))
                {
                    return new Paragraph(new Run(text));
                }
                return null;

            case HtmlNodeType.Element:
                var tagName = node.Name.ToLowerInvariant();

                if (tagName == "code")
                {
                    // code 标签内的内容
                    var para = new Paragraph();
                    foreach (var child in node.ChildNodes)
                    {
                        var inline = ConvertNodeToInline(child);
                        if (inline != null)
                        {
                            para.Inlines.Add(inline);
                        }
                    }
                    return para;
                }
                else if (tagName == "br")
                {
                    return new Paragraph(new LineBreak());
                }
                else if (tagName == "span")
                {
                    // 处理 span 包裹的内容
                    var para = new Paragraph();
                    foreach (var child in node.ChildNodes)
                    {
                        var inline = ConvertNodeToInline(child);
                        if (inline != null)
                        {
                            para.Inlines.Add(inline);
                        }
                    }
                    return para;
                }
                else
                {
                    // 其他标签，递归处理
                    var container = new Paragraph();
                    foreach (var child in node.ChildNodes)
                    {
                        var inline = ConvertNodeToInline(child);
                        if (inline != null)
                        {
                            container.Inlines.Add(inline);
                        }
                    }
                    return container.Inlines.Count > 0 ? container : null;
                }

            default:
                return null;
        }
    }

    private static Table CreateTable(HtmlNode node)
    {
        var table = new Table
        {
            Margin = new Thickness(0, 8, 0, 8),
            CellSpacing = 0
        };

        var firstRow = node.SelectSingleNode(".//tr");
        if (firstRow != null)
        {
            int colCount = firstRow.SelectNodes("th|td")?.Count ?? 1;
            for (int i = 0; i < colCount; i++)
            {
                table.Columns.Add(new TableColumn());
            }
        }

        var rowGroup = new TableRowGroup();

        var thead = node.SelectSingleNode("thead");
        if (thead != null)
        {
            foreach (var row in thead.SelectNodes("tr"))
            {
                rowGroup.Rows.Add(CreateTableRow(row, true));
            }
        }

        var tbody = node.SelectSingleNode("tbody");
        if (tbody != null)
        {
            foreach (var row in tbody.SelectNodes("tr"))
            {
                rowGroup.Rows.Add(CreateTableRow(row, false));
            }
        }

        if (thead == null && tbody == null)
        {
            foreach (var row in node.SelectNodes("tr"))
            {
                rowGroup.Rows.Add(CreateTableRow(row, false));
            }
        }

        table.RowGroups.Add(rowGroup);

        return table;
    }

    private static TableRow CreateTableRow(HtmlNode rowNode, bool isHeader)
    {
        var row = new TableRow();

        var cells = rowNode.SelectNodes("th|td");
        if (cells != null)
        {
            foreach (var cellNode in cells)
            {
                var cell = new TableCell
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(0.5),
                    Padding = new Thickness(8, 4, 8, 4)
                };

                if (isHeader)
                {
                    cell.FontWeight = FontWeights.Bold;
                    cell.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                }

                foreach (var child in cellNode.ChildNodes)
                {
                    var block = ConvertNodeToBlock(child);
                    if (block != null)
                    {
                        cell.Blocks.Add(block);
                    }
                }

                row.Cells.Add(cell);
            }
        }

        return row;
    }
}
