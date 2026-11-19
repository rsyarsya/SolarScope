namespace SolarScope;

internal static class SolarTheme
{
    public static readonly Color Green = Color.FromArgb(0x34, 0x79, 0x28);
    public static readonly Color LightGreen = Color.FromArgb(0xC0, 0xEB, 0xA6);
    public static readonly Color Cream = Color.FromArgb(0xFF, 0xFB, 0xE6);
    public static readonly Color Yellow = Color.FromArgb(0xFC, 0xCD, 0x2A);
    public static readonly Color Ink = Color.FromArgb(0x1F, 0x2A, 0x21);
    public static readonly Color MutedInk = Color.FromArgb(0x4F, 0x5E, 0x52);
    public static readonly Color Border = Color.FromArgb(0xD8, 0xE0, 0xD4);

    private static readonly Lazy<Bitmap> BrandImage = new(() =>
    {
        using var stream = typeof(SolarTheme).Assembly.GetManifestResourceStream("SolarScope.Brand.png")
            ?? throw new InvalidOperationException("Logo SolarScope tidak ditemukan.");
        using var bitmap = new Bitmap(stream);
        return new Bitmap(bitmap);
    });

    public static Image Logo => BrandImage.Value;

    public static Icon LoadAppIcon()
    {
        using var stream = typeof(SolarTheme).Assembly.GetManifestResourceStream("SolarScope.App.ico")
            ?? throw new InvalidOperationException("Ikon SolarScope tidak ditemukan.");
        return new Icon(stream);
    }
}
