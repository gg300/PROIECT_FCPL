using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal class HeroPanel : Panel
{
    private const string Line1Text = "Haine pentru oras.";
    private const string Line2Text = "Testate de un T-Rex.";
    private const string LeadText = "Alege cum vrei sa intri in magazin.";

    private readonly LogoBox _logo = new();

    private Font? _headFont;
    private Font? _leadFont;
    private Rectangle _plate;
    private Rectangle _line1;
    private Rectangle _line2;
    private Rectangle _lead;
    private Rectangle _bar;
    private TextFormatFlags _align = TextFormatFlags.HorizontalCenter;

    public HeroPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
        Controls.Add(_logo);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var card = Surface.CardBounds(this);
        int radius = LogicalToDeviceUnits(28);

        Surface.DrawShadow(g, card, radius, LogicalToDeviceUnits(12), LogicalToDeviceUnits(8));

        using var path = Theme.RoundedRect(card, radius);
        using (var fill = new LinearGradientBrush(card,
                   Theme.Mix(Theme.Ink, Color.White, 0.07f),
                   Theme.Mix(Theme.Ink, Color.Black, 0.35f),
                   LinearGradientMode.Vertical))
        {
            g.FillPath(fill, path);
        }

        var state = g.Save();
        g.SetClip(path, CombineMode.Intersect);
        PaintBackdrop(g);
        g.Restore(state);

        if (_headFont is null || _leadFont is null)
        {
            return;
        }

        const TextFormatFlags common = TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        TextRenderer.DrawText(g, Line1Text, _headFont, _line1, Color.White, _align | common);
        TextRenderer.DrawText(g, Line2Text, _headFont, _line2, Theme.Rex, _align | common);

        using (var bar = new SolidBrush(Theme.Rex))
        {
            g.FillRectangle(bar, _bar);
        }

        TextRenderer.DrawText(g, LeadText, _leadFont, _lead, Theme.Mix(Theme.Ink, Color.White, 0.68f),
            _align | TextFormatFlags.NoPadding | TextFormatFlags.WordBreak);
    }

    private void PaintBackdrop(Graphics g)
    {
        if (_plate.IsEmpty)
        {
            return;
        }

        float cx = _plate.X + _plate.Width / 2f;
        float cy = _plate.Y + _plate.Height / 2f;
        float half = _plate.Width / 2f;

        float glowR = half * 2.1f;
        using (var glowPath = new GraphicsPath())
        {
            glowPath.AddEllipse(cx - glowR, cy - glowR, glowR * 2, glowR * 2);
            using var glow = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(105, Theme.Rex),
                SurroundColors = new[] { Color.FromArgb(0, Theme.Rex) },
            };
            g.FillPath(glow, glowPath);
        }

        using (var ringPen = new Pen(Color.FromArgb(24, Color.White), 1.5f))
        {
            foreach (var k in new[] { 1.38f, 1.78f })
            {
                float r = half * k;
                g.DrawEllipse(ringPen, cx - r, cy - r, r * 2, r * 2);
            }
        }

        float orangeR = half + LogicalToDeviceUnits(10);
        using (var orange = new Pen(Theme.Rex, 3f))
        {
            g.DrawEllipse(orange, cx - orangeR, cy - orangeR, orangeR * 2, orangeR * 2);
        }

        using var plate = new LinearGradientBrush(_plate, Color.White, Theme.Concrete, LinearGradientMode.ForwardDiagonal);
        g.FillEllipse(plate, _plate);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        var card = Surface.CardBounds(this);
        if (card.Width < 80 || card.Height < 80)
        {
            return;
        }

        int pad = Math.Clamp(Math.Min(card.Width, card.Height) / 9,
            LogicalToDeviceUnits(24), LogicalToDeviceUnits(44));
        var inner = Rectangle.Inflate(card, -pad, -pad);
        bool horizontal = card.Width >= LogicalToDeviceUnits(560) && card.Width > card.Height * 1.3;
        int gap = LogicalToDeviceUnits(20);

        int plate = 0;
        int textX = inner.X;
        int textW = inner.Width;
        if (horizontal)
        {
            plate = Math.Max(LogicalToDeviceUnits(110), Math.Min(inner.Height, (int)(inner.Width * 0.4)));
            textX = inner.X + plate + pad;
            textW = Math.Max(LogicalToDeviceUnits(140), inner.Right - textX);
        }

        _headFont?.Dispose();
        _leadFont?.Dispose();
        _headFont = FitHeadlineFont(textW);
        _leadFont = new Font(Theme.FontName,
            Math.Clamp(_headFont.Size * 0.42f, LogicalToDeviceUnits(13), LogicalToDeviceUnits(18)),
            FontStyle.Regular, GraphicsUnit.Pixel);

        int headH = _headFont.Height;
        int leadH = TextRenderer.MeasureText(LeadText, _leadFont, new Size(textW, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.WordBreak).Height;
        int barW = LogicalToDeviceUnits(56);
        int barH = Math.Max(3, LogicalToDeviceUnits(5));
        int barGap = LogicalToDeviceUnits(12);
        int leadGap = LogicalToDeviceUnits(12);
        int blockH = headH * 2 + barGap + barH + leadGap + leadH;

        int blockY;
        if (horizontal)
        {
            _align = TextFormatFlags.Left;
            _plate = new Rectangle(inner.X, card.Y + (card.Height - plate) / 2, plate, plate);
            blockY = card.Y + (card.Height - blockH) / 2;
        }
        else
        {
            _align = TextFormatFlags.HorizontalCenter;
            int maxPlate = Math.Min(LogicalToDeviceUnits(340), (int)(inner.Width * 0.74));
            plate = Math.Min(maxPlate, inner.Height - blockH - gap);
            plate = Math.Max(plate, LogicalToDeviceUnits(96));

            int total = plate + gap + blockH;
            int y = card.Y + Math.Max(pad, (card.Height - total) / 2);
            _plate = new Rectangle(card.X + (card.Width - plate) / 2, y, plate, plate);
            blockY = y + plate + gap;
        }

        int logo = (int)(_plate.Width * 0.9);
        _logo.Size = new Size(logo, logo);
        _logo.Location = new Point(_plate.X + (_plate.Width - logo) / 2, _plate.Y + (_plate.Height - logo) / 2);

        int ty = blockY;
        _line1 = new Rectangle(textX, ty, textW, headH); ty += headH;
        _line2 = new Rectangle(textX, ty, textW, headH); ty += headH + barGap;
        _bar = new Rectangle(horizontal ? textX : textX + (textW - barW) / 2, ty, barW, barH); ty += barH + leadGap;
        _lead = new Rectangle(textX, ty, textW, leadH);

        Invalidate();
    }

    private Font FitHeadlineFont(int maxWidth)
    {
        float px = Math.Clamp(maxWidth / 11.5f, LogicalToDeviceUnits(22), LogicalToDeviceUnits(46));
        float min = LogicalToDeviceUnits(18);

        while (true)
        {
            var font = new Font(Theme.FontName, px, FontStyle.Bold, GraphicsUnit.Pixel);
            int width = TextRenderer.MeasureText(Line2Text, font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding).Width;

            if (width <= maxWidth || px <= min)
            {
                return font;
            }

            font.Dispose();
            px -= 1;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _headFont?.Dispose();
            _leadFont?.Dispose();
        }

        base.Dispose(disposing);
    }
}

internal class LinePanel : Panel
{
    private int _inset = -1;

    public bool LineOnTop { get; set; }

    public int Inset
    {
        get => _inset;
        set { _inset = value; Invalidate(); }
    }

    public LinePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int hair = Math.Max(1, LogicalToDeviceUnits(1));
        int accentH = LogicalToDeviceUnits(4);
        int accentW = LogicalToDeviceUnits(96);
        int hx = _inset >= 0 ? _inset : LogicalToDeviceUnits(48);

        int y = LineOnTop ? 0 : Height - hair;
        using var line = new SolidBrush(Theme.Line);
        e.Graphics.FillRectangle(line, 0, y, Width, hair);

        int ay = LineOnTop ? 0 : Height - accentH;
        using var accent = new SolidBrush(Theme.Rex);
        e.Graphics.FillRectangle(accent, hx, ay, accentW, accentH);
    }
}
