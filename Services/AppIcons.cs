using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace VolKey.Services;

internal static class AppIcons
{
    public static Icon Load(Size size)
    {
        using var stream = typeof(AppIcons).Assembly.GetManifestResourceStream("VolKey.ico")!;
        return new Icon(stream, size);
    }

    /// <summary>Бледная серая версия иконки для состояния «жесты выключены».</summary>
    public static Icon CreateDisabled(Icon icon)
    {
        using var source = icon.ToBitmap();
        using var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(result))
        using (var attributes = new ImageAttributes())
        {
            attributes.SetColorMatrix(new ColorMatrix(
            [
                [0.30f, 0.30f, 0.30f, 0, 0],
                [0.59f, 0.59f, 0.59f, 0, 0],
                [0.11f, 0.11f, 0.11f, 0, 0],
                [0, 0, 0, 0.75f, 0],
                [0, 0, 0, 0, 1]
            ]));
            graphics.DrawImage(source, new Rectangle(Point.Empty, source.Size),
                0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }

        var handle = result.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(handle).Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);
}
