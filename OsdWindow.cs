using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace VolKey;

internal static class OsdGlyph
{
    public const string Next = "";
    public const string Previous = "";
    public const string Play = "";
    public const string Pause = "";
    public const string Muted = "";
    public const string Volume = "";
}

/// <summary>
/// Плашка у верхнего края экрана, которая показывает выполненное действие и трек.
/// Не забирает фокус, пропускает клики сквозь себя и сама исчезает.
/// </summary>
internal sealed class OsdWindow : Form
{
    private const int WsExTopmost = 0x00000008;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExNoActivate = 0x08000000;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private const double VisibleOpacity = 0.97;

    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 2000 };
    private readonly System.Windows.Forms.Timer _fadeTimer = new() { Interval = 15 };
    private readonly Font _glyphFont;
    private readonly Font _titleFont = new("Segoe UI Semibold", 11F);
    private readonly Font _subtitleFont = new("Segoe UI", 9.5F);
    private string _glyph = "";
    private string _title = "";
    private string? _subtitle;
    private Image? _cover;

    public OsdWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
        Opacity = VisibleOpacity;
        _glyphFont = new Font(IsFontInstalled("Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets", 18F);

        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            _fadeTimer.Start();
        };
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity -= 0.08;
            if (Opacity > 0.05) return;
            _fadeTimer.Stop();
            Hide();
            Opacity = VisibleOpacity;
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExTopmost | WsExTransparent | WsExToolWindow | WsExNoActivate;
            return parameters;
        }
    }

    /// <param name="cover">Обложка трека; окно забирает её себе и освобождает само.</param>
    public void ShowMessage(string glyph, string title, string? subtitle = null, Image? cover = null)
    {
        _glyph = glyph;
        _title = title;
        _subtitle = string.IsNullOrWhiteSpace(subtitle) ? null : subtitle;
        _cover?.Dispose();
        _cover = cover;

        LayoutWindow();
        _fadeTimer.Stop();
        Opacity = VisibleOpacity;
        if (!Visible) Show();
        Invalidate();
        _hideTimer.Stop();
        _hideTimer.Start();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Скруглённые углы в стиле Windows 11; в Windows 10 вызов просто ничего не делает.
        var preference = DwmwcpRound;
        DwmSetWindowAttribute(Handle, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Theme.OsdBackground);
        using (var border = new Pen(Theme.OsdBorder))
            g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

        var padding = Dpi(14);
        var glyphBox = new Rectangle(padding, 0, Dpi(28), Height);
        TextRenderer.DrawText(g, _glyph, _glyphFont, glyphBox, Theme.OsdText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        var coverSize = _cover is null ? 0 : Height - padding * 2 + Dpi(8);
        if (_cover is not null)
        {
            var coverRect = new Rectangle(Width - padding - coverSize, (Height - coverSize) / 2, coverSize, coverSize);
            using var path = RoundedRectangle(coverRect, Dpi(6));
            g.SetClip(path);
            g.DrawImage(_cover, coverRect);
            g.ResetClip();
        }

        var textLeft = glyphBox.Right + Dpi(12);
        var textRight = Width - padding - (coverSize > 0 ? coverSize + Dpi(12) : 0);
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;
        if (_subtitle is null)
        {
            TextRenderer.DrawText(g, _title, _titleFont, new Rectangle(textLeft, 0, textRight - textLeft, Height),
                Theme.OsdText, flags | TextFormatFlags.VerticalCenter);
        }
        else
        {
            var titleHeight = TextRenderer.MeasureText(_title, _titleFont).Height;
            var subtitleHeight = TextRenderer.MeasureText(_subtitle, _subtitleFont).Height;
            var top = (Height - titleHeight - subtitleHeight) / 2;
            TextRenderer.DrawText(g, _title, _titleFont, new Rectangle(textLeft, top, textRight - textLeft, titleHeight), Theme.OsdText, flags);
            TextRenderer.DrawText(g, _subtitle, _subtitleFont, new Rectangle(textLeft, top + titleHeight, textRight - textLeft, subtitleHeight),
                Theme.OsdSecondaryText, flags);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer.Dispose();
            _fadeTimer.Dispose();
            _glyphFont.Dispose();
            _titleFont.Dispose();
            _subtitleFont.Dispose();
            _cover?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void LayoutWindow()
    {
        var height = Dpi(_cover is null ? 56 : 68);
        var textWidth = Math.Max(
            TextRenderer.MeasureText(_title, _titleFont).Width,
            _subtitle is null ? 0 : TextRenderer.MeasureText(_subtitle, _subtitleFont).Width);
        var coverWidth = _cover is null ? 0 : height - Dpi(28) + Dpi(8) + Dpi(12);
        var width = Dpi(14) + Dpi(28) + Dpi(12) + textWidth + coverWidth + Dpi(18);
        width = Math.Clamp(width, Dpi(200), Dpi(460));

        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Bounds = new Rectangle(area.Left + (area.Width - width) / 2, area.Top + Dpi(40), width, height);
    }

    private static GraphicsPath RoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static bool IsFontInstalled(string name)
    {
        using var fonts = new InstalledFontCollection();
        return fonts.Families.Any(family => family.Name == name);
    }

    private int Dpi(int value) => LogicalToDeviceUnits(value);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
