using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class LoyaltyView : ViewBase, ILiveView
{
    public LoyaltyView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "T-Rex Points"));

        if (Services.Session.CurrentUser is not { } user)
        {
            Ui.AddRow(column, Ui.Muted(this, "Programul de loialitate este disponibil doar cu cont."));
            Controls.Add(column);
            return;
        }

        var tier = LoyaltyService.TierFor(user);
        var next = LoyaltyService.NextTier(user);

        Ui.AddRow(column, new Label
        {
            Text = $"{user.LoyaltyPoints} puncte  =  {LoyaltyService.ValueOf(user.LoyaltyPoints):N2} lei reducere",
            AutoSize = true,
            Font = new Font(Theme.FontName, 16f, FontStyle.Bold),
            ForeColor = Theme.RexDark,
            Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(4)),
        });
        Ui.AddRow(column, Ui.Muted(this,
            $"Nivel: {LoyaltyService.TierName(tier.Tier)}  (x{tier.Multiplier:0.##} la punctele castigate)   |   " +
            $"Puncte castigate in total: {user.LifetimePoints}"));

        var progress = new ProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Height = LogicalToDeviceUnits(14),
            Width = LogicalToDeviceUnits(420),
            Style = ProgressBarStyle.Continuous,
            Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(4)),
        };

        string progressText;
        if (next is { } upcoming)
        {
            var span = upcoming.Next.FromLifetimePoints - tier.FromLifetimePoints;
            var done = user.LifetimePoints - tier.FromLifetimePoints;
            progress.Value = span <= 0 ? 100 : Math.Clamp(done * 100 / span, 0, 100);
            progressText = $"Mai ai nevoie de {upcoming.Missing} puncte pana la nivelul {LoyaltyService.TierName(upcoming.Next.Tier)}.";
        }
        else
        {
            progress.Value = 100;
            progressText = "Ai atins nivelul maxim. Multumim pentru fidelitate!";
        }

        Ui.AddRow(column, progress);
        Ui.AddRow(column, Ui.Muted(this, progressText));

        var gap = LogicalToDeviceUnits(24);

        var left = Stack(new Padding(0, 0, gap, 0));
        Ui.AddRow(left, Ui.Section(this, "Niveluri"));
        var tiers = Ui.Grid(this, ("Nivel", 30f, false), ("De la (puncte castigate)", 40f, true), ("Bonus", 30f, true));
        foreach (var info in LoyaltyService.Tiers)
        {
            var index = tiers.Rows.Add(
                LoyaltyService.TierName(info.Tier) + (info.Tier == tier.Tier ? "  (nivelul tau)" : string.Empty),
                info.FromLifetimePoints.ToString(),
                $"x{info.Multiplier:0.##}");

            if (info.Tier == tier.Tier)
            {
                tiers.Rows[index].DefaultCellStyle.Font = Ui.BoldFont;
            }
        }

        tiers.ClearSelection();
        tiers.Height = LogicalToDeviceUnits(42) * 4 + 2;
        tiers.Dock = DockStyle.Top;
        Ui.AddRow(left, tiers);

        Ui.AddRow(left, Ui.Section(this, "Cum functioneaza"));
        var rules = Ui.Muted(this,
            $"• La crearea contului primesti {LoyaltyService.WelcomeBonus} de puncte bonus." + Environment.NewLine +
            $"• Primesti 1 punct la fiecare {LoyaltyService.LeiPerEarnedPoint:0} lei platiti, cand comanda este livrata, " +
            "inmultit cu bonusul nivelului." + Environment.NewLine +
            $"• La finalizarea comenzii, 1 punct = {LoyaltyService.PointValue:N2} lei; poti plati cu puncte cel mult " +
            $"{LoyaltyService.MaxRedeemShare:P0} din suma." + Environment.NewLine +
            "• Punctele se cumuleaza cu cupoanele si cu reducerea de abonament." + Environment.NewLine +
            "• La un retur, punctele castigate pe produsele returnate se anuleaza; rambursarea in puncte " +
            $"primeste +{ReturnService.PointsRefundBonus:P0} bonus.");
        rules.MaximumSize = new Size(LogicalToDeviceUnits(480), 0);
        Ui.AddRow(left, rules, fill: true);

        var right = Stack(Padding.Empty);
        Ui.AddRow(right, Ui.Section(this, "Istoric"));
        var history = Ui.Grid(this, ("Data", 24f, false), ("Puncte", 16f, true), ("Detalii", 60f, false));
        foreach (var entry in Services.Loyalty.GetHistory())
        {
            var index = history.Rows.Add(
                entry.Date.ToString("dd-MM-yyyy HH:mm"),
                entry.Points > 0 ? $"+{entry.Points}" : entry.Points.ToString(),
                entry.Reason);
            history.Rows[index].Cells[1].Style.ForeColor = entry.Points > 0 ? Theme.Success : Ui.Danger;
        }

        if (history.Rows.Count == 0)
        {
            history.Rows.Add("-", "0", "Inca nu ai puncte. Le primesti cand iti este livrata prima comanda.");
        }

        Ui.AddRow(right, history, fill: true);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, LogicalToDeviceUnits(8), 0, 0),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(left, 0, 0);
        body.Controls.Add(right, 1, 0);

        Ui.AddRow(column, body, fill: true);
        Controls.Add(column);
    }

    private static TableLayoutPanel Stack(Padding margin)
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = margin,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }
}
