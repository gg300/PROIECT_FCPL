using System.Drawing.Drawing2D;

namespace MetroREX.Controls;

internal static class Surface
{
    public static Padding ShadowPadding(Control host) => new(
        host.LogicalToDeviceUnits(14),
        host.LogicalToDeviceUnits(4),
        host.LogicalToDeviceUnits(14),
        host.LogicalToDeviceUnits(24));

    public static Rectangle CardBounds(Control host)
    {
        var pad = ShadowPadding(host);
        return new Rectangle(
            pad.Left,
            pad.Top,
            Math.Max(1, host.Width - pad.Horizontal),
            Math.Max(1, host.Height - pad.Vertical));
    }

    public static void DrawShadow(Graphics g, RectangleF rect, float radius, int spread, int offsetY)
    {
        for (var i = spread; i >= 1; i--)
        {
            var layer = rect;
            layer.Inflate(i, i);
            layer.Offset(0, offsetY);

            int alpha = Math.Max(1, (int)(7f * (1f - (i - 1f) / spread)));
            using var path = Theme.RoundedRect(layer, radius + i);
            using var brush = new SolidBrush(Color.FromArgb(alpha, 22, 22, 26));
            g.FillPath(brush, path);
        }
    }
}
