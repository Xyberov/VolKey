namespace VolKey;

/// <summary>Цвета, которые не берутся из системной палитры, — свои для светлой и тёмной темы.</summary>
internal static class Theme
{
    public static bool IsDark => Application.IsDarkModeEnabled;

    public static Color Success => IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 16);
    public static Color Error => IsDark ? Color.FromArgb(255, 153, 164) : Color.FromArgb(196, 43, 28);
    public static Color Accent => IsDark ? Color.FromArgb(96, 205, 255) : Color.FromArgb(0, 95, 184);

    public static Color OsdBackground => IsDark ? Color.FromArgb(44, 44, 44) : Color.FromArgb(249, 249, 249);
    public static Color OsdBorder => IsDark ? Color.FromArgb(70, 70, 70) : Color.FromArgb(222, 222, 222);
    public static Color OsdText => IsDark ? Color.White : Color.FromArgb(26, 26, 26);
    public static Color OsdSecondaryText => IsDark ? Color.FromArgb(200, 200, 200) : Color.FromArgb(96, 96, 96);
}
