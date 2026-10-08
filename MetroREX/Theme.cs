using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace MetroREX;

/// <summary>Culorile si fonturile aplicatiei. Schimbi aici, se schimba peste tot.</summary>
internal static class Theme
{
    // Aceleasi culori ca in varianta web
    public static readonly Color Concrete = ColorTranslator.FromHtml("#E9ECEF"); // fundal
    public static readonly Color Paper    = Color.White;                         // carduri
    public static readonly Color Ink      = ColorTranslator.FromHtml("#16202C"); // text, contururi
    public static readonly Color Muted    = ColorTranslator.FromHtml("#5A6573"); // text secundar
    public static readonly Color Rex      = ColorTranslator.FromHtml("#2F8F5B"); // linia verde
    public static readonly Color Signal   = ColorTranslator.FromHtml("#F2B705"); // focus tastatura

    // Overpass daca e instalat, altfel Segoe UI (vine cu Windows)
    public static readonly string FontName = PickFont();

    public static readonly Font Body       = new(FontName, 10f);
    public static readonly Font Wordmark   = new(FontName, 18f, FontStyle.Bold);
    public static readonly Font Headline   = new(FontName, 26f, FontStyle.Bold);
    public static readonly Font Lead       = new(FontName, 12f);
    public static readonly Font StationTitle = new(FontName, 15f, FontStyle.Bold);
    public static readonly Font StationInfo  = new(FontName, 10.5f);

    private static string PickFont()
    {
        using var installed = new InstalledFontCollection();
        return installed.Families.Any(f => f.Name == "Overpass") ? "Overpass" : "Segoe UI";
    }

    /// <summary>Dreptunghi cu colturi rotunjite.</summary>
    public static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
