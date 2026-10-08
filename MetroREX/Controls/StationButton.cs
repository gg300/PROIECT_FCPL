using System.Drawing.Drawing2D;

namespace MetroREX.Controls;


internal class StationButton : Control
{
    private bool _hover;

    public string Title { get; set; } = "";
    public string Info { get; set; } = "";
    public bool IsGuest { get; set; }

    /// <summary>True cand mouse-ul e deasupra sau are focus (cercul statiei se face verde).</summary>
    public bool IsActive => _hover || Focused;

    /// <summary>Anunta parintele sa redeseneze cercul statiei.</summary>
    public event EventHandler? VisualStateChanged;

    // Spatiu lasat in jurul cardului pentru umbra si conturul de focus
    private int MarginLeft   => LogicalToDeviceUnits(10);
    private int MarginTop    => LogicalToDeviceUnits(5);
    private int MarginRight  => LogicalToDeviceUnits(10);
    private int MarginBottom => LogicalToDeviceUnits(8);
    private int Shadow       => LogicalToDeviceUnits(6);
    private int Shift        => LogicalToDeviceUnits(4);
    private int Radius       => LogicalToDeviceUnits(14);

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
    }

    /// <summary>Centrul vertical al cardului (pentru alinierea cercului).</summary>
    public int CardCenterY => MarginTop + CardHeight / 2;
    private int CardHeight => Height - MarginTop - MarginBottom - 1;

    private RectangleF CardRect() => new(
        MarginLeft + (_hover ? Shift : 0),
        MarginTop,
        Width - MarginLeft - MarginRight - 1,
        CardHeight);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var card = CardRect();

        // Umbra verde la hover
        if (_hover)
        {
            var shadowRect = new RectangleF(card.X - Shadow, card.Y + Shadow, card.Width, card.Height);
            using var shadowPath = Theme.RoundedRect(shadowRect, Radius);
            using var shadowBrush = new SolidBrush(Theme.Rex);
            g.FillPath(shadowBrush, shadowPath);
        }

        // Cardul alb + contur (punctat pentru „fara cont”)
        using (var path = Theme.RoundedRect(card, Radius))
        using (var paper = new SolidBrush(Theme.Paper))
        using (var border = new Pen(Theme.Ink, 2) { DashStyle = IsGuest ? DashStyle.Dash : DashStyle.Solid })
        {
            g.FillPath(paper, path);
            g.DrawPath(border, path);
        }

        // Contur galben doar cand focusul vine din tastatura (Tab)
        if (Focused && ShowFocusCues)
        {
            var focusRect = card;
            focusRect.Inflate(4, 4);
            using var focusPath = Theme.RoundedRect(focusRect, Radius + 4);
            using var focusPen = new Pen(Theme.Signal, 3);
            g.DrawPath(focusPen, focusPath);
        }

        // Textul: titlu + descriere
        var text = Rectangle.Round(card);
        text.Inflate(-LogicalToDeviceUnits(20), -LogicalToDeviceUnits(12));

        int titleHeight = TextRenderer.MeasureText(g, Title, Theme.StationTitle).Height;
        TextRenderer.DrawText(g, Title, Theme.StationTitle,
            new Rectangle(text.X, text.Y, text.Width, titleHeight),
            Theme.Ink, TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(g, Info, Theme.StationInfo,
            new Rectangle(text.X, text.Y + titleHeight + 2, text.Width, text.Height - titleHeight - 2),
            Theme.Muted, TextFormatFlags.Left | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    // ===== Starea vizuala =====
    private void SetHover(bool value)
    {
        if (_hover == value) return;
        _hover = value;
        Invalidate();
        VisualStateChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); SetHover(true); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); SetHover(false); }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
        VisualStateChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
        VisualStateChanged?.Invoke(this, EventArgs.Empty);
    }

    // ===== Enter / Space apasa butonul, ca la un buton normal =====
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
}
