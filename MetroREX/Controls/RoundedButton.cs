using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal enum ButtonKind
{
    Primary,    // solid orange
    Secondary,  // white with a light border
    Card,       // left-aligned white card (title + muted detail)
    Ghost,      // transparent, for dark headers (white text, subtle hover)
    Tile,       // big black tile that turns orange and lifts on hover
}

internal sealed class RoundedButton : Button
{
    private bool _hover;
    private bool _down;
    private bool _selected;

    public ButtonKind Kind { get; set; } = ButtonKind.Secondary;

    public bool Arrow { get; set; }

    public bool Selected
    {
        get => _selected;
        set { _selected = value; Invalidate(); }
    }

    public RoundedButton()
    {
        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        UseVisualStyleBackColor = false;
    }

    private int Radius => LogicalToDeviceUnits(10);
    private int ArrowWidth => LogicalToDeviceUnits(14);
    private int ArrowGap => LogicalToDeviceUnits(8);

    private string DisplayText(out bool hasArrow)
    {
        var text = Text.Replace('\u2212', '-');
        var typed = text.StartsWith('\u2190');
        hasArrow = Arrow || typed;
        return typed ? text[1..].TrimStart() : text;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        var text = DisplayText(out var hasArrow);
        var size = TextRenderer.MeasureText(text, Font);
        int extra = hasArrow ? ArrowWidth + ArrowGap : 0;
        return new Size(
            size.Width + extra + Padding.Horizontal + LogicalToDeviceUnits(24),
            Math.Max(MinimumSize.Height, size.Height + LogicalToDeviceUnits(16)));
    }

    private Color Backdrop()
    {
        for (var c = Parent; c is not null; c = c.Parent)
        {
            if (c.BackColor.A == 255)
            {
                return c.BackColor;
            }
        }

        return Theme.Concrete;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Backdrop());

        switch (Kind)
        {
            case ButtonKind.Tile: PaintTile(g); return;
            case ButtonKind.Ghost: PaintGhost(g); return;
        }

        // Symmetric rectangle: the 1.5px border is centred 0.75px inside every edge
        var rect = new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f);
        if (_down && Enabled)
        {
            rect.Offset(0, 1);
        }

        Color fill, border, fore;
        float borderWidth = 1.5f;
        if (Kind == ButtonKind.Primary)
        {
            fill = !Enabled ? Theme.Mix(Theme.Rex, Theme.Concrete, 0.6f)
                 : _down ? Theme.RexDark
                 : _hover ? Theme.Mix(Theme.Rex, Color.White, 0.18f)
                 : Theme.Rex;
            border = fill;
            fore = Enabled ? Theme.Ink : Theme.Muted;
        }
        else
        {
            fill = !Enabled ? Theme.Concrete
                 : _down ? Theme.Mix(Theme.RexSoft, Theme.Rex, 0.35f)
                 : _hover ? Theme.RexSoft
                 : Theme.Paper;
            border = !Enabled ? Theme.Line : _hover || _down ? Theme.Rex : Theme.Line;
            fore = Enabled ? Theme.Ink : Theme.Muted;
            if (Kind == ButtonKind.Card && (_hover || _down))
            {
                borderWidth = 2f;
            }
        }

        using (var path = Theme.RoundedRect(rect, Radius))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, borderWidth))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        PaintFocusRing(g, rect);

        var text = DisplayText(out var hasArrow);
        if (Kind == ButtonKind.Card)
        {
            PaintCardText(g, rect, text, fore);
        }
        else
        {
            PaintCenteredText(g, rect, text, fore, hasArrow);
        }
    }

    private void PaintGhost(Graphics g)
    {
        var rect = new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f);

        if (Enabled && (_hover || _down))
        {
            using var path = Theme.RoundedRect(rect, Radius);
            using var brush = new SolidBrush(Theme.Mix(Theme.Ink, Color.White, _down ? 0.24f : 0.13f));
            g.FillPath(brush, path);
        }

        PaintFocusRing(g, rect, Color.White);

        var text = DisplayText(out var hasArrow);
        var fore = Enabled ? Color.White : Theme.Muted;
        PaintCenteredText(g, rect, text, fore, hasArrow);

        if (Selected)
        {
            int total = ContentWidth(g, text, hasArrow);
            int x = (Width - total) / 2;
            int h = Math.Max(3, LogicalToDeviceUnits(3));
            using var bar = new SolidBrush(Theme.Rex);
            g.FillRectangle(bar, x, Height - h - LogicalToDeviceUnits(4), total, h);
        }
    }

    private void PaintTile(Graphics g)
    {
        int s = LogicalToDeviceUnits(6); // room reserved on the right / bottom for the hard shadow
        bool active = Enabled && (_hover || _down);

        var card = new RectangleF(1, 1, Width - s - 2, Height - s - 2);
        if (_down)
        {
            card.Offset(s / 2f, s / 2f);
        }

        if (active && !_down)
        {
            var shadow = card;
            shadow.Offset(s, s);
            using var shadowPath = Theme.RoundedRect(shadow, Radius + 2);
            using var shadowBrush = new SolidBrush(Theme.Ink);
            g.FillPath(shadowBrush, shadowPath);
        }
        else if (_down)
        {
            var shadow = card;
            shadow.Offset(s / 2f, s / 2f);
            using var shadowPath = Theme.RoundedRect(shadow, Radius + 2);
            using var shadowBrush = new SolidBrush(Theme.Ink);
            g.FillPath(shadowBrush, shadowPath);
        }

        using (var path = Theme.RoundedRect(card, Radius + 2))
        using (var brush = new SolidBrush(active ? Theme.Rex : Theme.Ink))
        using (var pen = new Pen(Theme.Ink, 2f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        PaintFocusRing(g, card, Theme.Signal, 3f);

        var lines = Text.Split('\n', 2, StringSplitOptions.TrimEntries);
        int padX = LogicalToDeviceUnits(22);
        int padY = LogicalToDeviceUnits(18);
        var bounds = Rectangle.Round(card);
        var titleColor = active ? Theme.Ink : Color.White;
        var detailColor = active ? Theme.Ink : Theme.Rex;

        using var titleFont = new Font(Font.FontFamily, 19f, FontStyle.Bold);
        using var infoFont = new Font(Font.FontFamily, 10f, FontStyle.Bold);
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        var huge = new Size(int.MaxValue, int.MaxValue);

        int infoH = 0;
        if (lines.Length > 1)
        {
            infoH = TextRenderer.MeasureText(g, lines[1], infoFont, huge, TextFormatFlags.NoPadding).Height;
            TextRenderer.DrawText(g, lines[1], infoFont,
                new Rectangle(bounds.X + padX, bounds.Y + padY, bounds.Width - 2 * padX - ArrowWidth * 2, infoH),
                detailColor, flags);
        }

        int titleH = TextRenderer.MeasureText(g, lines[0], titleFont, huge, TextFormatFlags.NoPadding).Height;
        TextRenderer.DrawText(g, lines[0], titleFont,
            new Rectangle(bounds.X + padX, bounds.Bottom - padY - titleH, bounds.Width - 2 * padX, titleH),
            titleColor, flags);

        float ax = bounds.Right - padX - ArrowWidth;
        float ay = bounds.Y + padY + Math.Max(infoH, ArrowWidth) / 2f;
        float head = ArrowWidth * 0.42f;
        using var arrowPen = new Pen(titleColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLine(arrowPen, ax, ay, ax + ArrowWidth, ay);
        g.DrawLines(arrowPen, new[]
        {
            new PointF(ax + ArrowWidth - head, ay - head),
            new PointF(ax + ArrowWidth, ay),
            new PointF(ax + ArrowWidth - head, ay + head),
        });
    }

    private void PaintFocusRing(Graphics g, RectangleF rect, Color? color = null, float inset = 3f)
    {
        if (!(Focused && ShowFocusCues))
        {
            return;
        }

        var ring = rect;
        ring.Inflate(-inset, -inset);
        using var ringPath = Theme.RoundedRect(ring, Math.Max(2, Radius - 3));
        using var ringPen = new Pen(color ?? Theme.Ink, 1.5f) { DashStyle = DashStyle.Dot };
        g.DrawPath(ringPen, ringPath);
    }

    private int ContentWidth(Graphics g, string text, bool hasArrow)
    {
        int textWidth = TextRenderer.MeasureText(g, text, Font, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding).Width;
        return hasArrow ? ArrowWidth + ArrowGap + textWidth : textWidth;
    }

    private void PaintCenteredText(Graphics g, RectangleF rect, string text, Color fore, bool hasArrow)
    {
        var bounds = Rectangle.Round(rect);
        if (!hasArrow)
        {
            TextRenderer.DrawText(g, text, Font, bounds, fore,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            return;
        }

        int total = ContentWidth(g, text, true);
        int textWidth = total - ArrowWidth - ArrowGap;
        int x = bounds.X + (bounds.Width - total) / 2;
        int cy = bounds.Y + bounds.Height / 2;

        float head = ArrowWidth * 0.42f;
        using (var pen = new Pen(fore, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
        {
            g.DrawLine(pen, x + 1, cy, x + ArrowWidth, cy);
            g.DrawLines(pen, new[]
            {
                new PointF(x + 1 + head, cy - head),
                new PointF(x + 1, cy),
                new PointF(x + 1 + head, cy + head),
            });
        }

        TextRenderer.DrawText(g, text, Font,
            new Rectangle(x + ArrowWidth + ArrowGap, bounds.Y, textWidth + 4, bounds.Height), fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private void PaintCardText(Graphics g, RectangleF rect, string text, Color fore)
    {
        var lines = text.Split('\n', 2, StringSplitOptions.TrimEntries);
        int padX = LogicalToDeviceUnits(22);
        var bounds = Rectangle.Round(rect);

        using var titleFont = new Font(Font.FontFamily, 14f, FontStyle.Bold);
        using var infoFont = new Font(Font.FontFamily, 10f, FontStyle.Regular);
        var huge = new Size(int.MaxValue, int.MaxValue);

        int titleH = TextRenderer.MeasureText(g, lines[0], titleFont, huge, TextFormatFlags.NoPadding).Height;
        int infoH = lines.Length > 1
            ? TextRenderer.MeasureText(g, lines[1], infoFont, huge, TextFormatFlags.NoPadding).Height
            : 0;
        int gap = lines.Length > 1 ? LogicalToDeviceUnits(4) : 0;

        int y = bounds.Y + (bounds.Height - (titleH + gap + infoH)) / 2;
        int w = bounds.Width - 2 * padX;
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        TextRenderer.DrawText(g, lines[0], titleFont, new Rectangle(bounds.X + padX, y, w, titleH), fore, flags);
        if (lines.Length > 1)
        {
            TextRenderer.DrawText(g, lines[1], infoFont,
                new Rectangle(bounds.X + padX, y + titleH + gap, w, infoH),
                Enabled ? Theme.Muted : Theme.Line, flags);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        // Navigation buttons and category tiles tick softly when pressed
        if (Kind is ButtonKind.Tile or ButtonKind.Ghost)
        {
            Sfx.Play(Sound.Tick);
        }

        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _down = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _down = false; Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
}
