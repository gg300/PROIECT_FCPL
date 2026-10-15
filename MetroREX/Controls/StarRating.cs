using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

/// <summary>Cinci stele desenate (nu caractere de font). Interactiva sau doar pentru afisare.</summary>
internal sealed class StarRating : Control
{
    private const int Count = 5;

    private readonly int _star;
    private readonly int _gap;
    private int _value;
    private int _hover;
    private bool _readOnly;

    public event EventHandler<int>? Rated;

    public StarRating(int starSize)
    {
        _star = Math.Max(10, starSize);
        _gap = Math.Max(2, _star / 6);

        SetStyle(ControlStyles.UserPaint
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.SupportsTransparentBackColor
               | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new Size(Count * _star + (Count - 1) * _gap, _star + 2);
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public int Value
    {
        get => _value;
        set { _value = Math.Clamp(value, 0, Count); Invalidate(); }
    }

    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            Cursor = value ? Cursors.Default : Cursors.Hand;
            TabStop = !value;
            Invalidate();
        }
    }

    private int IndexAt(Point p)
    {
        if (p.X < 0 || p.X >= Width)
        {
            return 0;
        }

        return Math.Clamp(p.X / (_star + _gap) + 1, 1, Count);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        int lit = !_readOnly && _hover > 0 ? _hover : _value;
        using var fill = new SolidBrush(Theme.Rex);
        using var litPen = new Pen(Theme.Rex, 1.5f) { LineJoin = LineJoin.Round };
        using var emptyPen = new Pen(Theme.Muted, 1.5f) { LineJoin = LineJoin.Round };

        for (var i = 0; i < Count; i++)
        {
            var points = StarPoints(i * (_star + _gap) + _star / 2f, 1 + _star / 2f, _star / 2f - 1f);
            if (i < lit)
            {
                g.FillPolygon(fill, points);
                g.DrawPolygon(litPen, points);
            }
            else
            {
                g.DrawPolygon(emptyPen, points);
            }
        }

        if (Focused && ShowFocusCues)
        {
            using var ring = new Pen(Theme.Ink, 1f) { DashStyle = DashStyle.Dot };
            g.DrawRectangle(ring, 0, 0, Width - 1, Height - 1);
        }
    }

    private static PointF[] StarPoints(float cx, float cy, float outer)
    {
        var inner = outer * 0.42f;
        var points = new PointF[10];
        for (var i = 0; i < 10; i++)
        {
            var radius = i % 2 == 0 ? outer : inner;
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            points[i] = new PointF(cx + (float)(Math.Cos(angle) * radius), cy + (float)(Math.Sin(angle) * radius));
        }

        return points;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_readOnly)
        {
            return;
        }

        var index = IndexAt(e.Location);
        if (index != _hover)
        {
            _hover = index;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = 0;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!_readOnly && e.Button == MouseButtons.Left)
        {
            Choose(IndexAt(e.Location));
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (!_readOnly && e.KeyChar is >= '1' and <= '5')
        {
            e.Handled = true;
            Choose(e.KeyChar - '0');
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    private void Choose(int stars)
    {
        if (stars < 1)
        {
            return;
        }

        _value = stars;
        _hover = 0;
        Invalidate();
        Rated?.Invoke(this, stars);
    }
}
