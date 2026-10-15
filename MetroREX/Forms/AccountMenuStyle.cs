using System.Drawing.Drawing2D;

namespace MetroREX.Forms;

/// <summary>
/// Dark, rounded styling for the account dropdown (Comenzi active ... Logout) so it matches the header
/// and cards: Ink background, Rex-orange hover with an accent bar, scaled for DPI.
/// </summary>
internal static class AccountMenuStyle
{
    /// <summary>Put this in <see cref="ToolStripItem.Tag"/> to render an item in the danger color (e.g. Logout).</summary>
    public const string Danger = "danger";

    public static void Apply(ContextMenuStrip menu)
    {
        ArgumentNullException.ThrowIfNull(menu);

        menu.Renderer = new Renderer();
        menu.BackColor = Theme.Ink;
        menu.ForeColor = Color.White;
        menu.Font = new Font(Theme.FontName, 10.5f);
        menu.ShowImageMargin = false;
        menu.ShowCheckMargin = false;
        menu.Cursor = Cursors.Hand;
        menu.DropShadowEnabled = false; // incompatible with a rounded Region
        menu.Padding = new Padding(menu.LogicalToDeviceUnits(6));
        menu.MinimumSize = new Size(menu.LogicalToDeviceUnits(220), 0);

        foreach (ToolStripItem item in menu.Items)
        {
            switch (item)
            {
                case ToolStripSeparator separator:
                    separator.Margin = new Padding(
                        menu.LogicalToDeviceUnits(4), menu.LogicalToDeviceUnits(4),
                        menu.LogicalToDeviceUnits(4), menu.LogicalToDeviceUnits(4));
                    break;
                default:
                    item.Padding = new Padding(
                        menu.LogicalToDeviceUnits(14), menu.LogicalToDeviceUnits(8),
                        menu.LogicalToDeviceUnits(14), menu.LogicalToDeviceUnits(8));
                    item.Margin = new Padding(0, 1, 0, 1);
                    break;
            }
        }

        // Rounded corners: re-clip whenever the size changes (items added, DPI changes, reopening).
        menu.SizeChanged += (_, _) => Round(menu);
        menu.Opening += (_, _) => Round(menu);
        menu.Opened += (_, _) => Round(menu);
    }

    private static void Round(ToolStripDropDown menu)
    {
        if (menu.Width <= 0 || menu.Height <= 0)
        {
            return;
        }

        var radius = menu.LogicalToDeviceUnits(12);
        using var path = Theme.RoundedRect(new Rectangle(0, 0, menu.Width, menu.Height), radius);
        var old = menu.Region;
        menu.Region = new Region(path);
        old?.Dispose();
    }

    private sealed class Renderer : ToolStripProfessionalRenderer
    {
        private static Color Edge => Theme.Mix(Theme.Ink, Color.White, 0.16f);

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var fill = new SolidBrush(Theme.Ink);
            e.Graphics.FillRectangle(fill, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            using var path = Theme.RoundedRect(bounds, e.ToolStrip.LogicalToDeviceUnits(12));
            using var pen = new Pen(Edge, 1f);
            g.DrawPath(pen, path);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // No icon gutter.
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Enabled || !(e.Item.Selected || e.Item.Pressed))
            {
                return;
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var danger = e.Item.Tag as string == Danger;
            var accent = danger ? Ui.Danger : Theme.Rex;
            var bounds = new Rectangle(Point.Empty, e.Item.Size);
            var radius = e.Item.Owner?.LogicalToDeviceUnits(8) ?? 8;

            using (var path = Theme.RoundedRect(bounds, radius))
            using (var fill = new SolidBrush(Color.FromArgb(e.Item.Pressed ? 70 : 42, accent)))
            {
                g.FillPath(fill, path);
            }

            var barWidth = Math.Max(3, e.Item.Owner?.LogicalToDeviceUnits(3) ?? 3);
            var barInset = bounds.Height / 4;
            using var bar = new SolidBrush(accent);
            g.FillRectangle(bar, new Rectangle(0, barInset, barWidth, bounds.Height - barInset * 2));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var danger = e.Item.Tag as string == Danger;
            var active = e.Item.Selected || e.Item.Pressed;

            e.TextColor = !e.Item.Enabled
                ? Theme.Muted
                : danger
                    ? (active ? Color.White : Ui.Danger)
                    : (active ? Color.White : Theme.Mix(Theme.Ink, Color.White, 0.82f));
            e.TextFont = active ? Ui.BoldFont : e.Item.Font;

            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.Height / 2;
            using var pen = new Pen(Edge, 1f);
            e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
        }
    }
}
