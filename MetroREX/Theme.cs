using System.Drawing.Drawing2D;

namespace MetroREX;

/// <summary>
/// Palette and typography taken from the MetroREX logo: street black, rex orange, clean white.
/// Every color / font used anywhere in the UI lives here.
/// </summary>
internal static class Theme
{
    // ===== Colors =====
    public static readonly Color Concrete = ColorTranslator.FromHtml("#F2F0EB"); // app background (warm light grey)
    public static readonly Color Paper    = Color.White;                          // cards, inputs, grids
    public static readonly Color Ink      = ColorTranslator.FromHtml("#16161A"); // text, headers, borders
    public static readonly Color Muted    = ColorTranslator.FromHtml("#6E6B66"); // secondary text
    public static readonly Color Rex      = ColorTranslator.FromHtml("#F7941D"); // brand orange (logo "REX")
    public static readonly Color RexDark  = ColorTranslator.FromHtml("#D97A00"); // orange for text / pressed
    public static readonly Color RexSoft  = ColorTranslator.FromHtml("#FFEBCF"); // light orange: hover / selection
    public static readonly Color Signal   = ColorTranslator.FromHtml("#FFC83D"); // keyboard focus ring
    public static readonly Color Line     = ColorTranslator.FromHtml("#E2DED6"); // hairlines, borders
    public static readonly Color Success  = ColorTranslator.FromHtml("#1E7B4B"); // "done" messages
    public static readonly Color Zebra    = ColorTranslator.FromHtml("#FAF8F4"); // alternate grid rows

    // ===== Fonts =====
    public const string FontName = "Segoe UI";

    public static readonly Font Body         = new(FontName, 10f);
    public static readonly Font Wordmark     = new(FontName, 20f, FontStyle.Bold | FontStyle.Italic);
    public static readonly Font Headline     = new(FontName, 30f, FontStyle.Bold);
    public static readonly Font Lead         = new(FontName, 13f);
    public static readonly Font StationTitle = new(FontName, 14f, FontStyle.Bold);
    public static readonly Font StationInfo  = new(FontName, 10f);

    // ===== Helpers =====

    /// <summary>Blends two colors: t = 0 gives <paramref name="a"/>, t = 1 gives <paramref name="b"/>.</summary>
    public static Color Mix(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
                                                                         (int)(a.G + (b.G - a.G) * t),
                                                                         (int)(a.B + (b.B - a.B) * t));

    public static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float d = Math.Max(0.01f, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));

        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
