using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class NewReturnView : ViewBase
{
    private const int QuantityColumn = 4;
    private const int ReasonColumn = 5;

    private readonly ComboBox _order;
    private readonly ComboBox _method;
    private readonly DataGridView _grid;
    private readonly Label _estimate;
    private readonly Label _message;

    public NewReturnView(ShopForm shell, string? orderId = null) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Retur nou"));
        Ui.AddRow(column, Ui.Muted(this,
            "Completeaza in tabel cantitatea de returnat si motivul. Produsele cu cantitatea 0 raman la tine."));

        _message = Ui.Message(this);

        var form = Ui.Fields(this);
        form.Dock = DockStyle.None;
        form.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        form.MinimumSize = new Size(LogicalToDeviceUnits(620), 0);

        _order = Inputs.Choice();
        foreach (var order in Services.Returns.GetEligibleOrders())
        {
            _order.Items.Add(new OrderChoice(order, Services.Returns.DaysLeft(order)));
        }

        Ui.AddField(this, form, "Comanda", _order);

        _method = Inputs.Choice();
        _method.Items.Add(new MethodChoice(RefundMethod.Original, "Pe metoda de plata a comenzii (card / numerar)"));
        _method.Items.Add(new MethodChoice(RefundMethod.LoyaltyPoints,
            $"In T-Rex Points (+{ReturnService.PointsRefundBonus:P0} bonus)"));
        _method.SelectedIndex = 0;
        Ui.AddField(this, form, "Rambursare", _method);

        Ui.AddRow(column, form);
        Ui.AddRow(column, _message);

        _grid = Ui.Grid(this,
            ("Produs", 34f, false),
            ("Pret", 13f, true),
            ("Cumparat", 11f, true),
            ("Se poate returna", 13f, true),
            ("De returnat", 11f, true));

        var reason = new DataGridViewComboBoxColumn
        {
            HeaderText = "Motiv",
            FillWeight = 26f,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        reason.Items.AddRange(ReturnService.Reasons.Cast<object>().ToArray());
        _grid.Columns.Add(reason);

        _grid.ReadOnly = false;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.EditMode = DataGridViewEditMode.EditOnEnter;
        foreach (DataGridViewColumn gridColumn in _grid.Columns)
        {
            gridColumn.ReadOnly = gridColumn.Index is not (QuantityColumn or ReasonColumn);
        }

        _grid.Columns[QuantityColumn].DefaultCellStyle.BackColor = Theme.RexSoft;
        _grid.Columns[ReasonColumn].DefaultCellStyle.BackColor = Theme.RexSoft;

        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty && _grid.CurrentCell is DataGridViewComboBoxCell)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _grid.CellValueChanged += (_, _) => UpdateEstimate();
        _grid.DataError += (_, e) => e.ThrowException = false;
        Ui.AddRow(column, _grid, fill: true);

        _estimate = Ui.Section(this, string.Empty);
        Ui.AddRow(column, _estimate);

        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.SecondaryButton(this, "← Inapoi", (_, _) => Shell.Back()));
        actions.Controls.Add(Ui.PrimaryButton(this, "Trimite cererea de retur", (_, _) => Submit()));
        Ui.AddRow(column, actions);

        Controls.Add(column);

        _order.SelectedIndexChanged += (_, _) => LoadOrder();
        _method.SelectedIndexChanged += (_, _) => UpdateEstimate();

        if (_order.Items.Count == 0)
        {
            Ui.Say(_message, $"Nu ai comenzi livrate in ultimele {ReturnService.ReturnWindowDays} zile care pot fi returnate.", isError: true);
            _grid.Enabled = false;
            UpdateEstimate();
            return;
        }

        var preselect = _order.Items.Cast<OrderChoice>()
            .Select((choice, index) => (choice, index))
            .FirstOrDefault(pair => orderId is not null && pair.choice.Order.Id == orderId);
        _order.SelectedIndex = preselect.choice is null ? 0 : preselect.index;
    }

    private Order? SelectedOrder => (_order.SelectedItem as OrderChoice)?.Order;

    private RefundMethod SelectedMethod => (_method.SelectedItem as MethodChoice)?.Method ?? RefundMethod.Original;

    private void LoadOrder()
    {
        _grid.Rows.Clear();
        if (SelectedOrder is not { } order)
        {
            UpdateEstimate();
            return;
        }

        foreach (var returnable in Services.Returns.GetReturnableItems(order))
        {
            var item = returnable.Item;
            var index = _grid.Rows.Add(
                item.ProductName,
                $"{item.UnitPrice:N2} lei",
                item.Quantity.ToString(),
                returnable.Available.ToString(),
                "0");

            var row = _grid.Rows[index];
            row.Tag = returnable;

            if (returnable.Available == 0)
            {
                row.ReadOnly = true;
                row.DefaultCellStyle.ForeColor = Theme.Muted;
                row.Cells[QuantityColumn].Style.BackColor = Theme.Paper;
                row.Cells[ReasonColumn].Style.BackColor = Theme.Paper;
            }
        }

        Ui.Note(_message, string.Empty);
        UpdateEstimate();
    }

    private List<(ReturnableItem Item, int Quantity, string Reason)>? ReadLines(out string? error)
    {
        error = null;
        var lines = new List<(ReturnableItem, int, string)>();

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is not ReturnableItem item)
            {
                continue;
            }

            var text = Convert.ToString(row.Cells[QuantityColumn].Value)?.Trim() ?? string.Empty;
            if (text.Length == 0)
            {
                continue;
            }

            if (!int.TryParse(text, out var quantity) || quantity < 0)
            {
                error = $"Cantitatea pentru „{item.Item.ProductName}” trebuie sa fie un numar intreg (0 sau mai mare).";
                return null;
            }

            if (quantity > 0)
            {
                lines.Add((item, quantity, Convert.ToString(row.Cells[ReasonColumn].Value) ?? string.Empty));
            }
        }

        return lines;
    }

    private void UpdateEstimate()
    {
        var order = SelectedOrder;
        var lines = ReadLines(out _);

        if (order is null || lines is null || lines.Count == 0)
        {
            _estimate.Text = "Rambursare estimata: -";
            return;
        }

        var amount = ReturnService.RefundFor(order, lines.Select(line => (line.Item.Item.UnitPrice, line.Quantity)));
        _estimate.Text = SelectedMethod == RefundMethod.LoyaltyPoints
            ? $"Rambursare estimata: {ReturnService.PointsForRefund(amount)} T-Rex Points (in loc de {amount:N2} lei)"
            : $"Rambursare estimata: {amount:N2} lei";

        if (order.Subtotal > 0 && order.TotalCost < order.Subtotal)
        {
            _estimate.Text += "   (reducerile comenzii se scad proportional)";
        }
    }

    private void Submit()
    {
        _grid.EndEdit();

        if (SelectedOrder is not { } order)
        {
            Ui.Say(_message, "Alege comanda pe care vrei sa o returnezi.", isError: true);
            return;
        }

        var lines = ReadLines(out var error);
        if (lines is null)
        {
            Ui.Say(_message, error ?? "Cantitate invalida.", isError: true);
            return;
        }

        var request = lines
            .Select(line => new ReturnLineRequest(line.Item.Item.ProductId, line.Quantity, line.Reason))
            .ToList();

        var result = Services.Returns.Create(order.Id, request, SelectedMethod);
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Sfx.Play(Sound.Tick);
        Shell.RefreshNotificationCount();
        MessageBox.Show(
            FindForm(),
            $"Cererea de retur {result.Value.ShortId} a fost trimisa.{Environment.NewLine}" +
            $"Va fi aprobata maine, iar a doua zi dupa aprobare produsele revin in stoc si primesti rambursarea.",
            "MetroREX – Retur",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        Shell.Back();
    }

    private sealed record OrderChoice(Order Order, int DaysLeft)
    {
        public override string ToString() =>
            $"{Order.ShortId}: livrata la {Order.DeliveryDate:dd-MM-yyyy}, {Order.TotalCost:N2} lei " +
            $"({(DaysLeft == 0 ? "ultima zi de retur" : $"mai ai {DaysLeft} zile")})";
    }

    private sealed record MethodChoice(RefundMethod Method, string Text)
    {
        public override string ToString() => Text;
    }
}
