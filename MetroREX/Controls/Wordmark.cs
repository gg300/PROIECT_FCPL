using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

/// <summary>"Metro" in white and "REX" in orange, like the logo, for the dark header. Click it to go home.</summary>
internal sealed class Wordmark : Control
{
    private static readonly Font MarkFont = new(Theme.FontName, 24f, FontStyle.Bold | FontStyle.Italic);

    public Wordmark()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Ink;
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        var huge = new Size(int.MaxValue, int.MaxValue);
        const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.VerticalCenter;

        int metroWidth = TextRenderer.MeasureText(g, "Metro", MarkFont, huge, TextFormatFlags.NoPadding).Width;
        int x = LogicalToDeviceUnits(2);

        TextRenderer.DrawText(g, "Metro", MarkFont, new Rectangle(x, 0, metroWidth + 4, Height), Color.White, flags);
        TextRenderer.DrawText(g, "REX", MarkFont, new Rectangle(x + metroWidth - 2, 0, Width, Height), Theme.Rex, flags);
    }
}

/// <summary>Dark header bar with an orange "metro line" along the bottom edge.</summary>
internal sealed class HeaderBar : Panel
{
    public HeaderBar()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Ink;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int h = LogicalToDeviceUnits(4);
        using var brush = new SolidBrush(Theme.Rex);
        e.Graphics.FillRectangle(brush, 0, Height - h, Width, h);
    }
}
