using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal enum AccessIcon
{
    User,
    UserPlus,
    Bag,
}

internal enum AccessStyle
{
    Primary,    // negru, devine portocaliu la hover
    Secondary,  // alb cu contur fin
    Ghost,      // ca Secondary, dar pe fundal usor colorat
}

/// <summary>
/// Cardul alb din dreapta paginii de start: antet (supratitlu, titlu, subtitlu), randurile de acces
/// (<see cref="AccessOption"/>) si o nota cu avantajele contului. Inaltimea randurilor se adapteaza la spatiul
/// disponibil; <see cref="HeightFor"/> da inaltimea necesara cand cardul sta sub prezentare, pe ferestre inguste.
/// </summary>
internal sealed class AccessCard : Panel
{
    private const string OverlineText = "BINE AI VENIT";
    private const string TitleText = "Intra in magazin";
    private const string SubtitleText = "Cu cont sau ca vizitator, cumperi la fel de usor.";
    private const string NoteText =
        "Cu cont primesti istoric de comenzi, comenzi active, wish list, cupoane si abonamente la vanzatori.";

    private readonly List<AccessOption> _options = new();

    private Font? _overlineFont;
    private Font? _titleFont;
    private Font? _subtitleFont;
    private Font? _noteFont;
    private Rectangle _overline;
    private Rectangle _title;
    private Rectangle _subtitle;
    private Rectangle _note;
    private int _separatorY;

    public AccessCard()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
    }

    public AccessOption AddOption(string title, string info, AccessIcon icon, AccessStyle style)
    {
        var option = new AccessOption(title, info, icon, style);
        _options.Add(option);
        Controls.Add(option);
        PerformLayout();
        return option;
    }

    // ===== Dimensiuni =====

    private int RowGap => LogicalToDeviceUnits(12);
    private float OverlinePx => LogicalToDeviceUnits(12);
    private float SubtitlePx => LogicalToDeviceUnits(14);
    private float NotePx => LogicalToDeviceUnits(12);

    private int Pad(int cardWidth) => Math.Clamp(cardWidth / 14, LogicalToDeviceUnits(24), LogicalToDeviceUnits(40));

    private float TitlePx(int cardWidth) =>
        Math.Clamp(cardWidth * 0.065f, LogicalToDeviceUnits(24), LogicalToDeviceUnits(32));

    private static int LineHeight(float px) => (int)Math.Ceiling(px * 1.35f);

    /// <summary>Tot ce nu sunt randurile de acces: margini, antet, separator si nota.</summary>
    private int Chrome(int cardWidth) =>
        Pad(cardWidth) * 2
        + LineHeight(OverlinePx) + LogicalToDeviceUnits(8)
        + LineHeight(TitlePx(cardWidth)) + LogicalToDeviceUnits(6)
        + LineHeight(SubtitlePx) + LogicalToDeviceUnits(22)
        + LogicalToDeviceUnits(20) + 1 + LogicalToDeviceUnits(14)
        + 2 * LineHeight(NotePx);

    /// <summary>Inaltimea (cu umbra) necesara pentru o latime data, cu randuri de inaltime normala.</summary>
    public int HeightFor(int outerWidth)
    {
        var shadow = Surface.ShadowPadding(this);
        int count = _options.Count;

        return Chrome(Math.Max(1, outerWidth - shadow.Horizontal))
               + count * LogicalToDeviceUnits(84)
               + Math.Max(0, count - 1) * RowGap
               + shadow.Vertical;
    }

    // ===== Aranjare =====

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        var card = Surface.CardBounds(this);
        if (card.Width < 120 || card.Height < 120 || _options.Count == 0)
        {
            return;
        }

        DisposeFonts();
        float titlePx = TitlePx(card.Width);
        _overlineFont = new Font(Theme.FontName, OverlinePx, FontStyle.Bold, GraphicsUnit.Pixel);
        _titleFont = new Font(Theme.FontName, titlePx, FontStyle.Bold, GraphicsUnit.Pixel);
        _subtitleFont = new Font(Theme.FontName, SubtitlePx, FontStyle.Regular, GraphicsUnit.Pixel);
        _noteFont = new Font(Theme.FontName, NotePx, FontStyle.Regular, GraphicsUnit.Pixel);

        int pad = Pad(card.Width);
        int count = _options.Count;
        int chrome = Chrome(card.Width);
        int rowH = Math.Clamp((card.Height - chrome - (count - 1) * RowGap) / count,
            LogicalToDeviceUnits(72), LogicalToDeviceUnits(96));
        int content = chrome + count * rowH + (count - 1) * RowGap;

        int top = card.Y + Math.Max(0, (card.Height - content) / 2);
        int x = card.X + pad;
        int w = card.Width - 2 * pad;
        int y = top + pad;

        _overline = new Rectangle(x, y, w, LineHeight(OverlinePx));
        y += _overline.Height + LogicalToDeviceUnits(8);
        _title = new Rectangle(x, y, w, LineHeight(titlePx));
        y += _title.Height + LogicalToDeviceUnits(6);
        _subtitle = new Rectangle(x, y, w, LineHeight(SubtitlePx));
        y += _subtitle.Height + LogicalToDeviceUnits(22);

        // Fiecare rand are in jur loc pentru inelul de focus (4 px), care nu iese din card.
        int focus = LogicalToDeviceUnits(4);
        foreach (var option in _options)
        {
            option.SetBounds(x - focus, y - focus, w + 2 * focus, rowH + 2 * focus);
            y += rowH + RowGap;
        }

        y += LogicalToDeviceUnits(20) - RowGap;
        _separatorY = y;
        y += 1 + LogicalToDeviceUnits(14);
        _note = new Rectangle(x, y, w, 2 * LineHeight(NotePx));

        Invalidate();
    }

    // ===== Desenare =====

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var card = Surface.CardBounds(this);
        int radius = LogicalToDeviceUnits(24);

        Surface.DrawShadow(g, card, radius, LogicalToDeviceUnits(12), LogicalToDeviceUnits(8));

        using (var path = Theme.RoundedRect(card, radius))
        using (var fill = new SolidBrush(Theme.Paper))
        using (var border = new Pen(Theme.Line, 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        if (_titleFont is null || _overlineFont is null || _subtitleFont is null || _noteFont is null)
        {
            return;
        }

        const TextFormatFlags oneLine = TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
        TextRenderer.DrawText(g, OverlineText, _overlineFont, _overline, Theme.RexDark, oneLine);
        TextRenderer.DrawText(g, TitleText, _titleFont, _title, Theme.Ink, oneLine);
        TextRenderer.DrawText(g, SubtitleText, _subtitleFont, _subtitle, Theme.Muted, oneLine);

        using (var separator = new SolidBrush(Theme.Line))
        {
            g.FillRectangle(separator, _note.X, _separatorY, _note.Width, 1);
        }

        TextRenderer.DrawText(g, NoteText, _noteFont, _note, Theme.Muted,
            TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }

    private void DisposeFonts()
    {
        _overlineFont?.Dispose();
        _titleFont?.Dispose();
        _subtitleFont?.Dispose();
        _noteFont?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeFonts();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Un rand de acces: iconita intr-un patrat rotunjit, titlu, descriere pe maximum doua randuri si sageata.
/// Culorile se schimba lin la hover si la focus de la tastatura. Click, Enter si Space declanseaza <c>Click</c>.
/// </summary>
internal sealed class AccessOption : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly string _title;
    private readonly string _info;
    private readonly AccessIcon _icon;
    private readonly AccessStyle _style;

    private bool _hover;
    private bool _down;
    private float _t; // 0 = repaus, 1 = hover / focus

    public AccessOption(string title, string info, AccessIcon icon, AccessStyle style)
    {
        _title = title;
        _info = info;
        _icon = icon;
        _style = style;

        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        BackColor = Theme.Paper;
        Margin = Padding.Empty;

        _timer.Tick += (_, _) => Step();
    }

    private bool IsActive => _hover || (Focused && ShowFocusCues);

    private float Progress => 1f - (1f - _t) * (1f - _t);

    // ===== Animatie =====

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
    }

    private void Animate() => _timer.Start();

    // ===== Desenare =====

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Theme.Paper);

        var t = Progress;
        int inset = LogicalToDeviceUnits(4);
        var rect = new RectangleF(inset, inset + (_down ? 1 : 0), Width - 2 * inset - 1, Height - 2 * inset - 1);
        int radius = LogicalToDeviceUnits(14);

        Color fill, border, titleColor, infoColor, tileFill, iconColor, arrowColor;
        if (_style == AccessStyle.Primary)
        {
            fill = Theme.Mix(Theme.Ink, Theme.Rex, t);
            border = fill;
            titleColor = Theme.Mix(Color.White, Theme.Ink, t);
            infoColor = Theme.Mix(Theme.Mix(Theme.Ink, Color.White, 0.70f), Theme.Ink, t);
            tileFill = Theme.Mix(Theme.Mix(Theme.Ink, Color.White, 0.14f), Theme.Mix(Theme.Rex, Theme.Ink, 0.18f), t);
            iconColor = Theme.Mix(Theme.Rex, Color.White, t);
            arrowColor = Theme.Mix(Theme.Rex, Theme.Ink, t);
        }
        else
        {
            var rest = _style == AccessStyle.Ghost ? Theme.Zebra : Theme.Paper;
            fill = Theme.Mix(rest, Theme.Mix(Theme.Paper, Theme.RexSoft, 0.55f), t);
            border = Theme.Mix(Theme.Line, Theme.Ink, t);
            titleColor = Theme.Ink;
            infoColor = Theme.Muted;
            tileFill = Theme.Mix(Theme.RexSoft, Theme.Rex, t);
            iconColor = Theme.Mix(Theme.RexDark, Theme.Ink, t);
            arrowColor = Theme.Ink;
        }

        using (var path = Theme.RoundedRect(rect, radius))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, 1.5f))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        if (Focused && ShowFocusCues)
        {
            var ring = rect;
            ring.Inflate(LogicalToDeviceUnits(3), LogicalToDeviceUnits(3));
            using var ringPath = Theme.RoundedRect(ring, radius + 3);
            using var ringPen = new Pen(Theme.Signal, 2.5f);
            g.DrawPath(ringPen, ringPath);
        }

        int padX = LogicalToDeviceUnits(16);
        int arrowW = LogicalToDeviceUnits(18);

        // Iconita
        float tileSize = Math.Clamp(rect.Height * 0.56f, LogicalToDeviceUnits(40), LogicalToDeviceUnits(52));
        var tile = new RectangleF(rect.X + padX, rect.Y + (rect.Height - tileSize) / 2f, tileSize, tileSize);
        using (var tilePath = Theme.RoundedRect(tile, LogicalToDeviceUnits(12)))
        using (var tileBrush = new SolidBrush(tileFill))
        {
            g.FillPath(tileBrush, tilePath);
        }

        var iconBox = tile;
        iconBox.Inflate(-tileSize * 0.22f, -tileSize * 0.22f);
        DrawIcon(g, _icon, iconBox, iconColor);

        // Text
        int textX = (int)tile.Right + LogicalToDeviceUnits(14);
        int textW = (int)(rect.Right - padX - arrowW - LogicalToDeviceUnits(12)) - textX;
        float titlePx = Math.Clamp(rect.Height * 0.22f, LogicalToDeviceUnits(15), LogicalToDeviceUnits(18));
        float infoPx = Math.Clamp(rect.Height * 0.15f, LogicalToDeviceUnits(12), LogicalToDeviceUnits(14));
        using var titleFont = new Font(Theme.FontName, titlePx, FontStyle.Bold, GraphicsUnit.Pixel);
        using var infoFont = new Font(Theme.FontName, infoPx, FontStyle.Regular, GraphicsUnit.Pixel);

        if (textW > LogicalToDeviceUnits(40))
        {
            int titleH = titleFont.Height;
            int infoH = Math.Min(
                TextRenderer.MeasureText(g, _info, infoFont, new Size(textW, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height,
                infoFont.Height * 2);
            int gap = LogicalToDeviceUnits(3);
            int y = (int)(rect.Y + (rect.Height - (titleH + gap + infoH)) / 2f);

            TextRenderer.DrawText(g, _title, titleFont, new Rectangle(textX, y, textW, titleH), titleColor,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, _info, infoFont, new Rectangle(textX, y + titleH + gap, textW, infoH), infoColor,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        // Sageata (aluneca spre dreapta la hover)
        float ax = rect.Right - padX - arrowW + LogicalToDeviceUnits(3) * t;
        float ay = rect.Y + rect.Height / 2f;
        float head = arrowW * 0.38f;
        using var arrowPen = new Pen(arrowColor, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLine(arrowPen, ax, ay, ax + arrowW, ay);
        g.DrawLines(arrowPen, new[]
        {
            new PointF(ax + arrowW - head, ay - head),
            new PointF(ax + arrowW, ay),
            new PointF(ax + arrowW - head, ay + head),
        });
    }

    // ===== Iconite desenate vectorial (nu depind de fonturi) =====

    private static void DrawIcon(Graphics g, AccessIcon icon, RectangleF box, Color color)
    {
        float s = Math.Min(box.Width, box.Height);
        float x = box.X + (box.Width - s) / 2f;
        float y = box.Y + (box.Height - s) / 2f;

        using var pen = new Pen(color, Math.Max(1.8f, s * 0.09f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        switch (icon)
        {
            case AccessIcon.User:
                DrawUser(g, pen, x, y, s);
                break;

            case AccessIcon.UserPlus:
                DrawUser(g, pen, x - s * 0.06f, y, s * 0.94f);
                float cx = x + s * 0.84f;
                float cy = y + s * 0.30f;
                float arm = s * 0.11f;
                g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
                g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
                break;

            case AccessIcon.Bag:
                using (var body = Theme.RoundedRect(new RectangleF(x + s * 0.18f, y + s * 0.34f, s * 0.64f, s * 0.52f), s * 0.08f))
                {
                    g.DrawPath(pen, body);
                }

                g.DrawArc(pen, x + s * 0.33f, y + s * 0.10f, s * 0.34f, s * 0.50f, 180, 180);
                break;
        }
    }

    private static void DrawUser(Graphics g, Pen pen, float x, float y, float s)
    {
        g.DrawEllipse(pen, x + s * 0.32f, y + s * 0.10f, s * 0.36f, s * 0.36f);
        g.DrawArc(pen, x + s * 0.14f, y + s * 0.58f, s * 0.72f, s * 0.72f, 180, 180);
    }

    // ===== Stare =====

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Animate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; Animate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _down = true; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); _down = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Animate(); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Animate(); Invalidate(); }

    // Enter / Space se comporta ca la un buton normal
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
