using ScreenGrab;
using STranslate.Plugin;
using System.Drawing;

namespace STranslate.Core;

public class Screenshot : IScreenshot
{
    public Bitmap? GetScreenshot()
    {
        if (ScreenGrabber.IsCapturing)
            return default;
        var bitmap = ScreenGrabber.CaptureDialog(isAuxiliary: true);
        if (bitmap == null)
            return default;
        return bitmap;
    }

    public async Task<Bitmap?> GetScreenshotAsync()
    {
        if (ScreenGrabber.IsCapturing)
            return default;
        var bitmap = await ScreenGrabber.CaptureAsync(isAuxiliary: true);
        if (bitmap == null)
            return default;
        return bitmap;
    }
}
