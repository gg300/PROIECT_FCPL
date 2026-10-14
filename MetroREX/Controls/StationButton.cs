using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal class StationButton : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };

    private bool _hover;
    private bool _down;
    private float _t; // 0 = repaus, 1 = hover / focus

    public string Title { get; set; } = "";
    public string Info { get; set; } = "";
    public bool IsGuest { get; set; }

    public bool IsPrimary { get; set; }

    public bool IsActive => _hover || (Focused && ShowFocusCues);

    public float Progress => 1f - (1f - _t) * (1f - _t);

    public event EventHandler? VisualStateChanged;

    // Spatiu lasat in jurul cardului pentru umbra si conturul de focus
    private int MarginLeft   => LogicalToDeviceUnits(10);
    private int MarginTop    => LogicalToDeviceUnits(6);
    private int MarginRight  => LogicalToDeviceUnits(10);
    private int MarginBottom => LogicalToDeviceUnits(10);
    private int Shadow       => LogicalToDeviceUnits(6);
    private int Shift        => LogicalToDeviceUnits(6);
    private int Radius       => LogicalToDeviceUnits(16);

    public StationButton()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        BackColor = Theme.Concrete;

        _timer.Tick += (_, _) => Step();
    }

    public int CardCenterY => MarginTop + CardHeight / 2;

    public int CardLeft => MarginLeft + (int)(Shift * Progress);

    private int CardHeight => Math.Max(10, Height - MarginTop - MarginBottom - 1);

    private RectangleF CardRect(float t) => new(
        MarginLeft + Shift * t + (_down ? LogicalToDeviceUnits(2) : 0),
        MarginTop,
        Math.Max(10, Width - MarginLeft - MarginRight - Shift - 1),
        CardHeight);

    private void Step()
    {
        var target = IsActive ? 1f : 0f;
        var delta = target - _t;

        if (Math.Abs(delta) < 0.02f)
        {
            _t = target;
            _timer.Stop();
        }
        else
        {
            _t += delta * 0.35f;
        }

        Invalidate();
        VisualStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Animate() => _timer.Start();

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Parent?.BackColor ?? Theme.Concrete);

        var t = Progress;
        var card = CardRect(t);

        Color fill, titleColor, infoColor, arrowFill, arrowRing, arrowFore, shadowColor;
        if (IsPrimary)
        {
            fill = Theme.Mix(Theme.Ink, Theme.Rex, t);
            titleColor = Theme.Mix(Color.White, Theme.Ink, t);
            infoColor = Theme.Mix(Theme.Mix(Theme.Ink, Color.White, 0.72f), Theme.Ink, t);
            arrowFill = Theme.Mix(Theme.Rex, Theme.Ink, t);
            arrowRing = Theme.Mix(Theme.Rex, Theme.Ink, t);
            arrowFore = Theme.Mix(Theme.Ink, Theme.Rex, t);
            shadowColor = Theme.Ink;
        }
        else
        {
            fill = Theme.Mix(Theme.Paper, Theme.RexSoft, t);
            titleColor = Theme.Ink;
            infoColor = Theme.Muted;
            arrowFill = Theme.Mix(Theme.Paper, Theme.Rex, t);
            arrowRing = Theme.Ink;
            arrowFore = Theme.Ink;
            shadowColor = Theme.Rex;
        }

        if (t > 0.02f)
        {
            var shadowRect = new RectangleF(card.X - Shadow * t, card.Y + Shadow * t, card.Width, card.Height);
            using var shadowPath = Theme.RoundedRect(shadowRect, Radius);
            using var shadowBrush = new SolidBrush(Color.FromArgb((int)(255 * t), shadowColor));
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var path = Theme.RoundedRect(card, Radius))
        using (var brush = new SolidBrush(fill))
        using (var border = new Pen(Theme.Ink, 2) { DashStyle = IsGuest ? DashStyle.Dash : DashStyle.Solid })
        {
            g.FillPath(brush, path);
            g.DrawPath(border, path);
        }

        // Contur galben doar cand focusul vine din tastatura (Tab)
        if (Focused && ShowFocusCues)
        {
            var ring = card;
            ring.Inflate(LogicalToDeviceUnits(4), LogicalToDeviceUnits(4));
            using var ringPath = Theme.RoundedRect(ring, Radius + 4);
            using var ringPen = new Pen(Theme.Signal, 3);
            g.DrawPath(ringPen, ringPath);
        }

        PaintContent(g, card, t, titleColor, infoColor, arrowFill, arrowRing, arrowFore);
    }

    private void PaintContent(Graphics g, RectangleF card, float t,
        Color titleColor, Color infoColor, Color arrowFill, Color arrowRing, Color arrowFore)
    {
        int padX = Math.Clamp((int)(card.Height * 0.2f), LogicalToDeviceUnits(16), LogicalToDeviceUnits(24));
        int d = Math.Clamp((int)(card.Height * 0.38f), LogicalToDeviceUnits(28), LogicalToDeviceUnits(42));

        int textW = (int)card.Width - 2 * padX - d - LogicalToDeviceUnits(12);
        if (textW < LogicalToDeviceUnits(40))
        {
            textW = (int)card.Width - 2 * padX;
        }

        float titlePx = Math.Clamp(card.Height * 0.20f, LogicalToDeviceUnits(16), LogicalToDeviceUnits(24));
        float infoPx = Math.Clamp(card.Height * 0.115f, LogicalToDeviceUnits(12), LogicalToDeviceUnits(15));
        using var titleFont = new Font(Theme.FontName, titlePx, FontStyle.Bold, GraphicsUnit.Pixel);
        using var infoFont = new Font(Theme.FontName, infoPx, FontStyle.Regular, GraphicsUnit.Pixel);

        int titleH = titleFont.Height;
        int infoH = Math.Min(
            TextRenderer.MeasureText(g, Info, infoFont, new Size(textW, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height,
            infoFont.Height * 2);
        int gap = LogicalToDeviceUnits(4);

        int x = (int)card.X + padX;
        int y = (int)(card.Y + (card.Height - (titleH + gap + infoH)) / 2f);

        TextRenderer.DrawText(g, Title, titleFont, new Rectangle(x, y, textW, titleH), titleColor,
            TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, Info, infoFont, new Rectangle(x, y + titleH + gap, textW, infoH), infoColor,
            TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        var circle = new RectangleF(card.Right - padX - d, card.Y + (card.Height - d) / 2f, d, d);
        using (var fillBrush = new SolidBrush(arrowFill))
        {
            g.FillEllipse(fillBrush, circle);
        }

        using (var ring = new Pen(arrowRing, 1.5f))
        {
            g.DrawEllipse(ring, circle);
        }

        float cx = circle.X + d / 2f + LogicalToDeviceUnits(2) * t;
        float cy = circle.Y + d / 2f;
        float half = d * 0.22f;
        float head = d * 0.16f;

        using var pen = new Pen(arrowFore, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLine(pen, cx - half, cy, cx + half, cy);
        g.DrawLines(pen, new[]
        {
            new PointF(cx + half - head, cy - head),
            new PointF(cx + half, cy),
            new PointF(cx + half - head, cy + head),
        });
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Animate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; Animate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _down = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _down = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Animate(); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Animate(); Invalidate(); }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Enter or Keys.Space || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Enter or Keys.Space)
        {
            e.Handled = true;
            OnClick(EventArgs.Empty);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
