using System.Reflection;
using System.Text.RegularExpressions;

namespace STranslate.Plugin;

/// <summary>
/// 提供用于获取程序集版本信息的静态方法。
/// </summary>
public static partial class VersionInfo
{
    /// <summary>
    /// 获取当前程序集的版本号。
    /// 优先返回语义化版本（InformationalVersion），否则返回主版本号。
    /// </summary>
    /// <returns>程序集的版本号字符串。</returns>
    public static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 优先使用 InformationalVersion（语义化版本）
        var informationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informationalVersion))
        {
            // 使用正则表达式提取标准的语义化版本号
            // 例如：1.0.0-alpha.1
            var match = VersionRegex().Match(informationalVersion);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        // 备选
        var version = assembly.GetName().Version;
        return version == null ? string.Empty : $"{version.Major}.{version.Minor}.{version.Build}";
    }

#if DEBUG
    [GeneratedRegex(@"^(\d+\.\d+\.\d+(?:-[a-zA-Z0-9\-\.]+)?)")]
#else
    [GeneratedRegex(@"^(\d+\.\d+\.\d+)")]
#endif
    private static partial Regex VersionRegex();
}