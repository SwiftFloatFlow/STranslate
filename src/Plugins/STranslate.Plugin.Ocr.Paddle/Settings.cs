using System.IO;

namespace STranslate.Plugin.Ocr.Paddle;

public class Settings
{
    private static string _defaultPath = string.Empty;
    public static string DefaultPath
    {
        get => _defaultPath;
        set
        {
            if (!Directory.Exists(value))
                Directory.CreateDirectory(value);

            _defaultPath = value;
        }
    }
    public string ModelsDirectory { get; set; } = Path.Combine(DefaultPath, "Models");
}