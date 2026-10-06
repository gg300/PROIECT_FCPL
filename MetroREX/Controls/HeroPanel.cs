namespace MetroREX.Controls;

/// <summary>Partea din stânga: logo, titlu pe două rânduri, text scurt.</summary>
internal class HeroPanel : Panel
{
    private readonly LogoBox _logo = new();
    private readonly Label _line1 = MakeHeadline("Haine pentru oraș.");
    private readonly Label _line2 = MakeHeadline("Testate de un T-Rex.");
    private readonly Label _lead = new()
    {
        Text = "Alege cum vrei să intri în magazin.",
        AutoSize = true,
        Font = Theme.Lead,
        ForeColor = Theme.Muted,
    };

    public HeroPanel()
    {
        DoubleBuffered = true;
        BackColor = Theme.Concrete;
        Controls.AddRange(new Control[] { _logo, _line1, _line2, _lead });
    }

    private static Label MakeHeadline(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Headline,
        ForeColor = Theme.Ink,
    };

    // Logo-ul se micșorează pe ferestre mici, iar totul rămâne centrat pe verticală
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);

        int maxLogo = LogicalToDeviceUnits(280);
        int logo = (int)Math.Min(maxLogo, Math.Min(ClientSize.Width * 0.7, ClientSize.Height * 0.45));
        logo = Math.Max(logo, LogicalToDeviceUnits(80));
        _logo.Size = new Size(logo, logo);

        int gapAfterLogo = LogicalToDeviceUnits(20);
        int gapBeforeLead = LogicalToDeviceUnits(10);

        int total = logo + gapAfterLogo + _line1.PreferredHeight + _line2.PreferredHeight
                    + gapBeforeLead + _lead.PreferredHeight;
        int y = Math.Max(0, (ClientSize.Height - total) / 2);

        _logo.Location  = new Point(0, y); y += logo + gapAfterLogo;
        _line1.Location = new Point(0, y); y += _line1.PreferredHeight;
        _line2.Location = new Point(0, y); y += _line2.PreferredHeight + gapBeforeLead;
        _lead.Location  = new Point(0, y);
    }
}

/// <summary>Panou cu o linie groasă sus sau jos (pentru header și footer).</summary>
internal class LinePanel : Panel
{
    public bool LineOnTop { get; set; }

    public LinePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var ink = new SolidBrush(Theme.Ink);
        int thickness = LogicalToDeviceUnits(2);
        int y = LineOnTop ? 0 : Height - thickness;
        e.Graphics.FillRectangle(ink, 0, y, Width, thickness);
    }
}
