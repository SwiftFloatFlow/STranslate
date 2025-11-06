using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using System.IO;

namespace STranslate.Core;

public static class Helper
{
    private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger>();

    public static bool ShouldDeleteDirectory(string directory)
        => File.Exists(Path.Combine(directory, "NeedDelete.txt"));

    public static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (Exception e)
        {
            _logger.LogError($"无法删除目录 <{directory}>: {e.Message}");
        }
    }

    public static string GetPluginDicrtoryName(PluginMetaData metaData)
        => metaData.IsPrePlugin ? metaData.AssemblyName : $"{metaData.AssemblyName}_{metaData.PluginID}";
}