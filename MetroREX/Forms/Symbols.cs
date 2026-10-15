using System.Runtime.InteropServices;

namespace MetroREX.Forms;

/// <summary>
/// Simboluri Unicode (stele, sageti etc.) cu rezerva ASCII. Fiecare simbol este verificat o singura data
/// pe fontul aplicatiei; daca fontul nu il contine (altfel apare un patrat gol), se foloseste varianta simpla.
/// </summary>
internal static class Symbols
{
    public static readonly string Bullet = Pick("\u2022", "-");          // •
    public static readonly string Dot = Pick("\u25CF", "[x]");           // ●
    public static readonly string DotEmpty = Pick("\u25CB", "[ ]");      // ○
    public static readonly string Rule = Pick("\u2500\u2500", "--");     // ──

    private static string Pick(params string[] candidates)
    {
        for (var i = 0; i < candidates.Length - 1; i++)
        {
            if (Supports(candidates[i]))
            {
                return candidates[i];
            }
        }

        return candidates[^1]; // ultima varianta este mereu ASCII
    }

    private static bool Supports(string text)
    {
        try
        {
            using var font = new Font(Theme.FontName, 10f);
            var hdc = GetDC(IntPtr.Zero);
            var hFont = font.ToHfont();
            var old = SelectObject(hdc, hFont);
            try
            {
                var indices = new ushort[text.Length];
                var count = GetGlyphIndicesW(hdc, text, text.Length, indices, GgiMarkNonexistingGlyphs);
                return count != GdiError && indices.All(index => index != 0xFFFF);
            }
            finally
            {
                SelectObject(hdc, old);
                DeleteObject(hFont);
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }
        catch
        {
            return false;
        }
    }

    private const uint GgiMarkNonexistingGlyphs = 1;
    private const uint GdiError = 0xFFFFFFFF;

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetGlyphIndicesW(IntPtr hdc, string text, int count, [Out] ushort[] indices, uint flags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
}
