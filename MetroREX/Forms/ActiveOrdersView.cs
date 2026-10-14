using MetroREX.Models;

namespace MetroREX.Forms;

internal sealed class ActiveOrdersView : ViewBase, ILiveView
{
    private readonly DataGridView _orders;
    private readonly DataGridView _items;
    private readonly Label _summary;

    public ActiveOrdersView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Comenzi active"));

        _summary = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _summary);

        _orders = Ui.Grid(this,
            ("Comanda", 16f, false),
            ("Plasata la", 18f, false),
            ("Livrare estimata", 18f, false),
            ("Produse", 12f, true),
            ("Plata", 14f, false),
            ("Total", 22f, true));
        _orders.SelectionChanged += (_, _) => ShowItems();

        _items = Ui.Grid(this,
            ("Produs", 46f, false),
            ("Pret", 18f, true),
            ("Cantitate", 14f, true),
            ("Total", 22f, true));

        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        split.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        split.Controls.Add(_orders, 0, 0);
        split.Controls.Add(Ui.Section(this, "Produsele comenzii selectate"), 0, 1);
        split.Controls.Add(_items, 0, 2);

        Ui.AddRow(column, split, fill: true);
        Controls.Add(column);
        Reload();
    }

    private void Reload()
    {
        var orders = Services.Orders.GetActiveOrders();

        _orders.Rows.Clear();
        foreach (var order in orders)
        {
            var index = _orders.Rows.Add(
                order.ShortId,
                order.OrderDate.ToString("dd-MM-yyyy HH:mm"),
                order.DeliveryDate.ToString("dd-MM-yyyy"),
                order.Items.Sum(item => item.Quantity).ToString(),
                order.PaymentMethod == PaymentMethod.Card ? "Card" : "Numerar",
                $"{order.TotalCost:N2} lei");
            _orders.Rows[index].Tag = order;
        }

        _summary.Text = orders.Count switch
        {
            0 => "Nu ai comenzi in curs de livrare.",
            1 => "1 comanda in curs de livrare.",
            _ => $"{orders.Count} comenzi in curs de livrare.",
        };

        ShowItems();
    }

    private void ShowItems()
    {
        _items.Rows.Clear();
        if (_orders.CurrentRow?.Tag is not Order order)
        {
            return;
        }

        foreach (var item in order.Items)
        {
            _items.Rows.Add(
                item.ProductName,
                $"{item.UnitPrice:N2} lei",
                item.Quantity.ToString(),
                $"{item.Total:N2} lei");
        }
    }
}
