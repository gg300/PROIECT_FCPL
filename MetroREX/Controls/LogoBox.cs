using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

/// <summary>
/// Afișează assets/logo.png. Dacă fișierul lipsește,
/// desenează un chenar punctat cu instrucțiunea.
/// </summary>
internal class LogoBox : Control
{
    public static readonly string LogoPath =
        Path.Combine(AppContext.BaseDirectory, "assets", "logo.png");

    private readonly Image? _image;

    public LogoBox()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Concrete;

        if (File.Exists(LogoPath))
        {
            // Copiem imaginea în memorie, ca fișierul să nu rămână blocat cât rulează aplicația
            using var stream = File.OpenRead(LogoPath);
            using var temp = Image.FromStream(stream);
            _image = new Bitmap(temp);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (_image is not null)
        {
            // Încadrare proporțională (ca object-fit: contain din CSS)
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            float scale = Math.Min((float)Width / _image.Width, (float)Height / _image.Height);
            float w = _image.Width * scale;
            float h = _image.Height * scale;
            g.DrawImage(_image, (Width - w) / 2, (Height - h) / 2, w, h);
            return;
        }

        // Placeholder
        var rect = new RectangleF(2, 2, Width - 5, Height - 5);
        using var path = Theme.RoundedRect(rect, LogicalToDeviceUnits(14));
        using var pen = new Pen(Theme.Muted, 3) { DashStyle = DashStyle.Dash };
        g.DrawPath(pen, path);

        TextRenderer.DrawText(g, "Pune logo-ul în\nassets/logo.png", Theme.Body,
            Rectangle.Round(rect), Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _image?.Dispose();
        base.Dispose(disposing);
    }
}
