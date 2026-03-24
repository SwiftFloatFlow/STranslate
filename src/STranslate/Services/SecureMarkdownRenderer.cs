using HtmlAgilityPack;
using Markdig;
using Microsoft.Extensions.Logging;

namespace STranslate.Services;

/// <summary>
/// 线程安全的 Markdown 渲染服务
/// </summary>
public class SecureMarkdownRenderer
{
    private static readonly Lazy<SecureMarkdownRenderer> _instance =
        new(() => new SecureMarkdownRenderer());

    public static SecureMarkdownRenderer Instance => _instance.Value;

    private readonly MarkdownPipeline _pipeline;
    private readonly HashSet<string> _allowedTags;
    private readonly Dictionary<string, HashSet<string>> _allowedAttributes;
    private readonly ILogger<SecureMarkdownRenderer>? _logger;

    private SecureMarkdownRenderer()
    {
        try
        {
            _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ILogger<SecureMarkdownRenderer>>();
        }
        catch
        {
            _logger = null;
        }

        _pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseEmphasisExtras()
            .UseAutoLinks()
            .UseListExtras()
            .UsePipeTables()
            .UseTaskLists()
            .Build();

        _allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "p", "div", "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "blockquote", "pre", "code",
            "table", "thead", "tbody", "tr", "th", "td",
            "hr", "br", "strong", "em", "b", "i",
            "a", "del", "span", "sup", "sub"
        };

        _allowedAttributes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
            ["code"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class" },
            ["pre"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class" }
        };
    }

    public string Render(string markdown, int maxLength = 10000, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        cancellationToken.ThrowIfCancellationRequested();

        if (markdown.Length > maxLength)
        {
            markdown = markdown[..maxLength] + "\n\n---\n**内容已截断**";
            _logger?.LogDebug("Markdown 内容已截断，原始长度: {OriginalLength}", markdown.Length);
        }

        string html;
        try
        {
            html = Markdig.Markdown.ToHtml(markdown, _pipeline);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Markdown 渲染失败，返回纯文本");
            return $"<pre>{System.Net.WebUtility.HtmlEncode(markdown)}</pre>";
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            html = SanitizeHtml(html, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HTML 净化失败");
            return $"<pre>{System.Net.WebUtility.HtmlEncode(markdown)}</pre>";
        }

        return html;
    }

    private string SanitizeHtml(string html, CancellationToken cancellationToken)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        if (doc.DocumentNode == null)
            return string.Empty;

        cancellationToken.ThrowIfCancellationRequested();
        SanitizeNode(doc.DocumentNode, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        return doc.DocumentNode.OuterHtml;
    }

    private void SanitizeNode(HtmlNode node, CancellationToken cancellationToken)
    {
        if (node.NodeType != HtmlNodeType.Element)
            return;

        // 先递归处理所有子节点
        for (int i = node.ChildNodes.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SanitizeNode(node.ChildNodes[i], cancellationToken);
        }

        var tagName = node.Name.ToLowerInvariant();

        if (!_allowedTags.Contains(tagName))
        {
            var parent = node.ParentNode;
            if (parent != null)
            {
                var children = node.ChildNodes.ToList();
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    parent.InsertBefore(children[i], node);
                }
                parent.RemoveChild(node);
            }
            return;
        }

        SanitizeAttributes(node, tagName);

        if (tagName == "a")
        {
            node.SetAttributeValue("rel", "noopener noreferrer nofollow");
            node.SetAttributeValue("target", "_blank");
        }
    }

    private void SanitizeAttributes(HtmlNode node, string tagName)
    {
        if (!_allowedAttributes.TryGetValue(tagName, out var allowedAttrs))
        {
            node.Attributes.RemoveAll();
            return;
        }

        for (int i = node.Attributes.Count - 1; i >= 0; i--)
        {
            var attr = node.Attributes[i];
            var attrName = attr.Name.ToLowerInvariant();

            if (!allowedAttrs.Contains(attrName))
            {
                node.Attributes.Remove(attr);
            }
            else if (attrName == "href")
            {
                var sanitizedUrl = SanitizeUrl(attr.Value);
                if (string.IsNullOrEmpty(sanitizedUrl))
                {
                    node.Attributes.Remove(attr);
                }
                else
                {
                    attr.Value = sanitizedUrl;
                }
            }
        }
    }

    private string SanitizeUrl(string url)
    {
        System.Diagnostics.Debug.WriteLine($"SanitizeUrl called with: {url}");
        
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        url = url.Trim();
        
        // 先解码URL，以便检测编码后的危险协议
        try
        {
            var decoded = Uri.UnescapeDataString(url);
            if (decoded != url)
            {
                System.Diagnostics.Debug.WriteLine($"URL decoded: {url} -> {decoded}");
                url = decoded;
            }
        }
        catch { }

        var lowerUrl = url.ToLowerInvariant();

        var dangerousProtocols = new[] { "javascript:", "vbscript:", "data:", "file:", "about:", "chrome:" };
        foreach (var protocol in dangerousProtocols)
        {
            if (lowerUrl.StartsWith(protocol))
            {
                System.Diagnostics.Debug.WriteLine($"Blocking dangerous protocol: {protocol}");
                _logger?.LogWarning("阻止危险 URL 协议: {Protocol}", protocol);
                return string.Empty;
            }
        }

        if (!lowerUrl.StartsWith("http://") &&
            !lowerUrl.StartsWith("https://") &&
            !lowerUrl.StartsWith("#"))
        {
            System.Diagnostics.Debug.WriteLine($"URL not allowed, returning empty: {url}");
            return string.Empty;
        }

        System.Diagnostics.Debug.WriteLine($"URL allowed: {url}");
        return url;
    }
}
