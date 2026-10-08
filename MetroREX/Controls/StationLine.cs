using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

/// <summary>
/// Meniul principal desenat ca o linie de metrou:
/// o linie verde verticala, cu cate un cerc in dreptul fiecarui buton.
/// </summary>
internal class StationLine : Panel
{
    private readonly List<StationButton> _stations = new();

    private int LineX         => LogicalToDeviceUnits(26);  // centrul liniei verzi
    private int Indent        => LogicalToDeviceUnits(64);  // unde incep cardurile
    private int StationHeight => LogicalToDeviceUnits(110);
    private int Gap           => LogicalToDeviceUnits(12);

    public StationLine()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
    }

    public StationButton AddStation(string title, string info, bool isGuest = false)
    {
        var station = new StationButton { Title = title, Info = info, IsGuest = isGuest };
        station.VisualStateChanged += (_, _) => Invalidate();
        _stations.Add(station);
        Controls.Add(station);
        PerformLayout();
        return station;
    }

    // Asaza butoanele unul sub altul, centrate pe verticala
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_stations.Count == 0) return;

        int total = _stations.Count * StationHeight + (_stations.Count - 1) * Gap;
        int y = Math.Max(0, (ClientSize.Height - total) / 2);

        foreach (var station in _stations)
        {
            station.SetBounds(Indent, y, Math.Max(200, ClientSize.Width - Indent), StationHeight);
            y += StationHeight + Gap;
        }
        Invalidate();
    }

    private int CenterY(StationButton s) => s.Top + s.CardCenterY;

    // Deseneaza linia verde si cercurile statiilor
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_stations.Count == 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var line = new Pen(Theme.Rex, LogicalToDeviceUnits(10))
               { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(line, LineX, CenterY(_stations[0]), LineX, CenterY(_stations[^1]));
        }

        int size = LogicalToDeviceUnits(28);
        int ring = LogicalToDeviceUnits(6);

        foreach (var station in _stations)
        {
            var circle = new Rectangle(LineX - size / 2, CenterY(station) - size / 2, size, size);

            using var fill = new SolidBrush(station.IsActive ? Theme.Rex : Theme.Paper);
            g.FillEllipse(fill, circle);

            circle.Inflate(-ring / 2, -ring / 2);
            using var border = new Pen(Theme.Ink, ring);
            g.DrawEllipse(border, circle);
        }
    }
}
