using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal class StationLine : Panel
{
    private readonly List<StationButton> _stations = new();

    private int LineX      => LogicalToDeviceUnits(24);  // centrul liniei
    private int Indent     => LogicalToDeviceUnits(52);  // unde incep cardurile
    private int Gap        => LogicalToDeviceUnits(8);
    private int MinStation => LogicalToDeviceUnits(92);
    private int MaxStation => LogicalToDeviceUnits(132);

    public StationLine()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Theme.Concrete;
    }

    public int DesiredHeight =>
        _stations.Count == 0 ? 0 : _stations.Count * LogicalToDeviceUnits(100) + (_stations.Count - 1) * Gap;

    public StationButton AddStation(string title, string info, bool isGuest = false, bool isPrimary = false)
    {
        var station = new StationButton { Title = title, Info = info, IsGuest = isGuest, IsPrimary = isPrimary };
        station.VisualStateChanged += (_, _) => Invalidate();
        _stations.Add(station);
        Controls.Add(station);
        PerformLayout();
        return station;
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_stations.Count == 0 || ClientSize.Width <= 0)
        {
            return;
        }

        int count = _stations.Count;
        int stationH = Math.Clamp((ClientSize.Height - (count - 1) * Gap) / count, MinStation, MaxStation);
        int total = count * stationH + (count - 1) * Gap;
        int y = Math.Max(0, (ClientSize.Height - total) / 2);
        int width = Math.Max(LogicalToDeviceUnits(200), ClientSize.Width - Indent);

        foreach (var station in _stations)
        {
            station.SetBounds(Indent, y, width, stationH);
            y += stationH + Gap;
        }

        Invalidate();
    }

    private static int CenterY(StationButton s) => s.Top + s.CardCenterY;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_stations.Count == 0)
        {
            return;
        }

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
            int cy = CenterY(station);

            // Legatura scurta de la cerc pana la card (urmareste alunecarea cardului)
            using (var link = new Pen(Theme.Ink, Math.Max(2, LogicalToDeviceUnits(3))))
            {
                g.DrawLine(link, LineX + size / 2, cy, station.Left + station.CardLeft, cy);
            }

            var circle = new Rectangle(LineX - size / 2, cy - size / 2, size, size);

            using var fill = new SolidBrush(Theme.Mix(Theme.Paper, Theme.Rex, station.Progress));
            g.FillEllipse(fill, circle);

            circle.Inflate(-ring / 2, -ring / 2);
            using var border = new Pen(Theme.Ink, ring);
            g.DrawEllipse(border, circle);
        }
    }
}
