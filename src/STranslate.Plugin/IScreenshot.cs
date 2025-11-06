using System.Drawing;

namespace STranslate.Plugin;

/// <summary>
/// 截图接口
/// </summary>
public interface IScreenshot
{
    /// <summary>
    /// 获取截图
    /// </summary>
    /// <returns></returns>
    Bitmap? GetScreenshot();
    
    /// <summary>
    /// 异步获取截图
    /// </summary>
    /// <returns></returns>
    Task<Bitmap?> GetScreenshotAsync();
}
