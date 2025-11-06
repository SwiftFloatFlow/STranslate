using STranslate.Plugin;
using System.IO;

namespace STranslate.Core;

public class PluginStorage<T> : StorageBase<T> where T : new()
{
    public PluginStorage(PluginMetaData metaData, string serviceId)
    {
        DirectoryPath = metaData.PluginSettingsDirectoryPath;
        EnsureDirectoryExists();

        FilePath = Path.Combine(DirectoryPath, $"{serviceId}{FileSuffix}");
    }

    public override void Save()
    {
        try
        {
            base.Save();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save ST settings to path: {FilePath}", e);
        }
    }

    public override async Task SaveAsync()
    {
        try
        {
            await base.SaveAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to save ST settings to path: {FilePath}", e);
        }
    }
}