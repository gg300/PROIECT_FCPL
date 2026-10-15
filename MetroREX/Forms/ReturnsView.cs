using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class ReturnsView : ViewBase, ILiveView
{
    private readonly DataGridView _returns;
    private readonly DataGridView _lines;
    private readonly Label _stages;
    private readonly Label _message;

    public ReturnsView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Retururi"));
        Ui.AddRow(column, Ui.Muted(this,
            $"Poti returna produse in {ReturnService.ReturnWindowDays} zile de la livrare. " +
            "Cererea se aproba a doua zi, iar a treia zi produsele revin in stoc si primesti banii (sau puncte, cu bonus)."));

        var eligible = Services.Returns.GetEligibleOrders().Count;
        var bar = Ui.Bar(this);
        bar.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(4));
        var newReturn = Ui.PrimaryButton(this, "Retur nou", (_, _) => Shell.Navigate(() => new NewReturnView(Shell)));
        newReturn.Enabled = eligible > 0;
        bar.Controls.Add(newReturn);
        bar.Controls.Add(new Label
        {
            Text = eligible > 0
                ? $"{eligible} comenzi livrate pot fi returnate."
                : "Nu ai comenzi livrate in termenul de retur.",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Margin = new Padding(LogicalToDeviceUnits(4), LogicalToDeviceUnits(12), 0, 0),
        });
        Ui.AddRow(column, bar);

        _message = Ui.Message(this);
        Ui.AddRow(column, _message);

        _returns = Ui.Grid(this,
            ("Retur", 12f, false),
            ("Comanda", 12f, false),
            ("Data cererii", 16f, false),
            ("Stare", 22f, false),
            ("Produse", 10f, true),
            ("Rambursare", 28f, true));
        _returns.SelectionChanged += (_, _) => ShowSelected();

        _stages = new Label
        {
            AutoSize = true,
            Font = Ui.BoldFont,
            ForeColor = Theme.Ink,
            Margin = new Padding(0, LogicalToDeviceUnits(10), 0, LogicalToDeviceUnits(6)),
        };

        _lines = Ui.Grid(this,
            ("Produs", 40f, false),
            ("Cantitate", 14f, true),
            ("Pret", 16f, true),
            ("Motiv", 30f, false));

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        split.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        split.Controls.Add(_returns, 0, 0);
        split.Controls.Add(_stages, 0, 1);
        split.Controls.Add(_lines, 0, 2);
        Ui.AddRow(column, split, fill: true);

        Controls.Add(column);
        Reload();
    }

    private void Reload()
    {
        var returns = Services.Returns.GetReturns();

        _returns.Rows.Clear();
        foreach (var request in returns)
        {
            var index = _returns.Rows.Add(
                request.ShortId,
                request.OrderShortId,
                request.RequestDate.ToString("dd-MM-yyyy"),
                ReturnService.StatusText(request.Status),
                request.Lines.Sum(line => line.Quantity).ToString(),
                RefundText(request));
            _returns.Rows[index].Tag = request;
        }

        Ui.Note(_message, returns.Count == 0 ? "Nu ai cereri de retur." : string.Empty);
        ShowSelected();
    }

    private void ShowSelected()
    {
        _lines.Rows.Clear();
        if (_returns.CurrentRow?.Tag is not ReturnRequest request)
        {
            _stages.Text = string.Empty;
            return;
        }

        _stages.Text = StagesText(request);
        foreach (var line in request.Lines)
        {
            _lines.Rows.Add(line.ProductName, line.Quantity.ToString(), $"{line.UnitPrice:N2} lei", line.Reason);
        }
    }

    private static string RefundText(ReturnRequest request) => request.RefundMethod == RefundMethod.LoyaltyPoints
        ? $"{request.RefundPoints} puncte ({request.RefundAmount:N2} lei)"
        : $"{request.RefundAmount:N2} lei";

    private static string StagesText(ReturnRequest request)
    {
        static string Stop(bool done, string name, DateTime? date) =>
            $"{(done ? Symbols.Dot : Symbols.DotEmpty)} {name}{(date is { } value ? $" ({value:dd-MM})" : string.Empty)}";

        return string.Join($"  {Symbols.Rule}  ", new[]
        {
            Stop(true, "Cerere", request.RequestDate),
            Stop(request.Status >= ReturnStatus.Approved, "Aprobat", request.ApprovedDate),
            Stop(request.Status == ReturnStatus.Completed, "Finalizat", request.CompletedDate),
        });
    }
}
