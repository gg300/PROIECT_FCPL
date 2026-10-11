using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

/// <summary>
/// Tile mare pentru o categorie: numar-fantoma in fundal, chip cu numarul de produse, sageata si
/// linia portocalie de "metrou". La hover cardul se ridica, devine portocaliu si totul se anima lin.
/// Dimensiunile fonturilor se adapteaza la marimea tile-ului (design responsive).
/// </summary>
internal sealed class CategoryTile : Control
{
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly string _title;
    private readonly string _detail;
    private string _index;

    private bool _hover;
    private bool _down;
    private float _t; // 0 = repaus, 1 = hover

    public CategoryTile(string title, string detail, int number)
    {
        _title = title;
        _detail = detail;
        _index = number.ToString("00");

        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.ResizeRedraw
               | ControlStyles.SupportsTransparentBackColor
               | ControlStyles.Selectable, true);
        TabStop = true;
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent; // fundalul (puncte, lumini) vine din pagina parinte
        Margin = Padding.Empty;

        _timer.Tick += (_, _) => Step();
    }

    /// <summary>Schimba numarul-fantoma (folosit cand lista se filtreaza sau se sorteaza).</summary>
    public void SetNumber(int number)
    {
        _index = number.ToString("00");
        Invalidate();
    }

    // ===== Animatie =====

    private void Step()
    {
        var target = _hover || Focused && ShowFocusCues ? 1f : 0f;
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

    private static float Ease(float t) => 1f - (1f - t) * (1f - t);

    // ===== Desenare =====

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        // Fundalul e desenat de parinte (BackColor transparent), deci nu mai stergem cu o culoare plata.

        var t = Ease(Math.Clamp(_t, 0f, 1f));
        int s = LogicalToDeviceUnits(8);   // spatiu rezervat pentru umbra
        int pad = Math.Clamp(Width / 14, LogicalToDeviceUnits(16), LogicalToDeviceUnits(26));
        int radius = LogicalToDeviceUnits(16);

        float w = Width - s - 2;
        float h = Height - s - 2;
        float offset = 1 + s / 2f - s / 2f * t + (_down ? s / 2f : 0f);

        var card = new RectangleF(offset, offset, w, h);
        var shadow = new RectangleF(1 + s, 1 + s, w, h);

        // Umbra "hard": portocalie in repaus, neagra la hover
        using (var shadowPath = Theme.RoundedRect(shadow, radius))
        using (var shadowBrush = new SolidBrush(Theme.Mix(Theme.Rex, Theme.Ink, t)))
        {
            g.FillPath(shadowBrush, shadowPath);
        }

        var fill = Theme.Mix(Theme.Ink, Theme.Rex, t);
        using var cardPath = Theme.RoundedRect(card, radius);
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(Theme.Ink, 2f))
        {
            g.FillPath(brush, cardPath);
            g.DrawPath(pen, cardPath);
        }

        var state = g.Save();
        g.SetClip(cardPath, CombineMode.Intersect);
        PaintDecor(g, card, fill, t);
        g.Restore(state);

        PaintChip(g, card, pad, t);
        PaintArrow(g, card, pad, t);
        PaintTitle(g, card, pad, t);

        if (Focused && ShowFocusCues)
        {
            var ring = card;
            ring.Inflate(-LogicalToDeviceUnits(5), -LogicalToDeviceUnits(5));
            using var ringPath = Theme.RoundedRect(ring, Math.Max(2, radius - 5));
            using var ringPen = new Pen(Theme.Signal, 2.5f) { DashStyle = DashStyle.Dot };
            g.DrawPath(ringPen, ringPath);
        }
    }

    /// <summary>Dungi diagonale (linii de metrou) si numarul-fantoma din fundal.</summary>
    private void PaintDecor(Graphics g, RectangleF card, Color fill, float t)
    {
        var stripe = Theme.Mix(fill, Color.White, 0.06f + 0.10f * t);
        float stripeW = LogicalToDeviceUnits(14);
        float gap = LogicalToDeviceUnits(30);
        using (var pen = new Pen(stripe, stripeW))
        {
            for (var i = 0; i < 3; i++)
            {
                float x = card.Right - LogicalToDeviceUnits(10) - i * (stripeW + gap) - card.Height * 0.15f * t;
                g.DrawLine(pen, x + card.Height, card.Y - 2, x - card.Height * 0.2f, card.Bottom + 2);
            }
        }

        var ghost = Theme.Mix(fill, Color.White, 0.07f + 0.18f * t);
        float px = Math.Max(LogicalToDeviceUnits(60), card.Height * 0.78f);
        using var font = new Font(Theme.FontName, px, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel);
        var size = TextRenderer.MeasureText(g, _index, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        var rect = new Rectangle(
            (int)(card.Right - size.Width - LogicalToDeviceUnits(10) + 14 * t),
            (int)(card.Bottom - size.Height * 0.84f),
            size.Width + LogicalToDeviceUnits(6),
            size.Height);
        TextRenderer.DrawText(g, _index, font, rect, ghost, TextFormatFlags.NoPadding | TextFormatFlags.Left);
    }

    /// <summary>Chipul cu numarul de produse, sus-stanga.</summary>
    private void PaintChip(Graphics g, RectangleF card, int pad, float t)
    {
        using var font = new Font(Theme.FontName, LogicalToDeviceUnits(12), FontStyle.Bold, GraphicsUnit.Pixel);
        var text = TextRenderer.MeasureText(g, _detail, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

        int padX = LogicalToDeviceUnits(11);
        int padY = LogicalToDeviceUnits(5);
        var chip = new RectangleF(card.X + pad, card.Y + pad, text.Width + padX * 2, text.Height + padY * 2);

        using var path = Theme.RoundedRect(chip, chip.Height / 2f);
        if (t > 0.01f)
        {
            using var alpha = new SolidBrush(Color.FromArgb((int)(255 * t), Theme.Ink));
            g.FillPath(alpha, path);
        }

        var line = Theme.Mix(Theme.Rex, Theme.Ink, t);
        using (var pen = new Pen(line, 1.5f))
        {
            g.DrawPath(pen, path);
        }

        var fore = Theme.Mix(Theme.Rex, Color.White, t);
        TextRenderer.DrawText(g, _detail, font,
            new Rectangle((int)chip.X + padX, (int)chip.Y + padY, text.Width + 4, text.Height),
            fore, TextFormatFlags.NoPadding | TextFormatFlags.Left);
    }

    /// <summary>Cercul cu sageata, sus-dreapta.</summary>
    private void PaintArrow(Graphics g, RectangleF card, int pad, float t)
    {
        float d = LogicalToDeviceUnits(36);
        var circle = new RectangleF(card.Right - pad - d, card.Y + pad - LogicalToDeviceUnits(3), d, d);

        if (t > 0.01f)
        {
            using var fillBrush = new SolidBrush(Color.FromArgb((int)(255 * t), Theme.Ink));
            g.FillEllipse(fillBrush, circle);
        }

        using (var ring = new Pen(Theme.Mix(Theme.Mix(Theme.Ink, Color.White, 0.35f), Theme.Ink, t), 1.5f))
        {
            g.DrawEllipse(ring, circle);
        }

        var color = Theme.Mix(Color.White, Theme.Rex, t);
        float cx = circle.X + d / 2f + LogicalToDeviceUnits(2) * t;
        float cy = circle.Y + d / 2f;
        float half = d * 0.22f;
        float head = d * 0.16f;

        using var pen = new Pen(color, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.DrawLine(pen, cx - half, cy, cx + half, cy);
        g.DrawLines(pen, new[]
        {
            new PointF(cx + half - head, cy - head),
            new PointF(cx + half, cy),
            new PointF(cx + half - head, cy + head),
        });
    }

    /// <summary>Linia de accent si numele categoriei, jos-stanga.</summary>
    private void PaintTitle(Graphics g, RectangleF card, int pad, float t)
    {
        float px = Math.Clamp(card.Height * 0.17f, LogicalToDeviceUnits(20), LogicalToDeviceUnits(32));
        using var font = new Font(Theme.FontName, px, FontStyle.Bold, GraphicsUnit.Pixel);

        int maxWidth = (int)card.Width - pad * 2;
        var size = TextRenderer.MeasureText(g, _title, font, new Size(maxWidth, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.WordBreak);
        int lines = Math.Min(2, Math.Max(1, (int)Math.Round(size.Height / (px * 1.3f))));
        int titleH = (int)(font.Height * lines);
        int x = (int)card.X + pad;
        int y = (int)card.Bottom - pad - titleH;

        // Linia portocalie (devine neagra si se lungeste la hover)
        float lineW = LogicalToDeviceUnits(28) + LogicalToDeviceUnits(44) * t;
        using (var line = new SolidBrush(Theme.Mix(Theme.Rex, Theme.Ink, t)))
        {
            g.FillRectangle(line, x, y - LogicalToDeviceUnits(14), lineW, Math.Max(3, LogicalToDeviceUnits(4)));
        }

        var fore = Theme.Mix(Color.White, Theme.Ink, t);
        TextRenderer.DrawText(g, _title, font, new Rectangle(x, y, maxWidth, titleH), fore,
            TextFormatFlags.Left | TextFormatFlags.Bottom | TextFormatFlags.NoPadding
            | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
    }

    // ===== Stare =====

    protected override void OnClick(EventArgs e)
    {
        Sfx.Play(Sound.Tick);
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Animate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; _down = false; Animate(); }
    protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); _down = true; Focus(); Invalidate(); }
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
