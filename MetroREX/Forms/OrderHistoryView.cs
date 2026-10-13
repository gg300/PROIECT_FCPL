using MetroREX.Models;

namespace MetroREX.Forms;

/// <summary>
/// Istoricul comenzilor livrate, in formatul din cerinta:
/// data livrarii, fiecare produs cu Pret / Cantitate / Total si costul total al comenzii.
/// </summary>
internal sealed class OrderHistoryView : ViewBase, ILiveView
{
    private readonly RichTextBox _text;
    private readonly Button _rateButton;
    private readonly Label _summary;

    public OrderHistoryView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Istoricul comenzilor"));

        _summary = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _summary);

        _text = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Paper,
            ForeColor = Theme.Ink,
            Font = new Font(Theme.FontName, 10.5f),
            DetectUrls = false,
            Margin = Padding.Empty,
        };

        // Rama alba cu padding in jurul textului.
        var frame = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Paper,
            Padding = new Padding(LogicalToDeviceUnits(16)),
            Margin = Padding.Empty,
        };
        frame.Controls.Add(_text);
        Ui.AddRow(column, frame, fill: true);

        var actions = Ui.Bar(this);
        _rateButton = Ui.PrimaryButton(this, "Evalueaza produsele", (_, _) => Rate());
        actions.Controls.Add(_rateButton);
        Ui.AddRow(column, actions);

        Controls.Add(column);
        Reload();
    }

    private void Reload()
    {
        var orders = Services.Orders.GetHistory();

        _summary.Text = orders.Count switch
        {
            0 => "Nu ai inca nicio comanda livrata.",
            1 => "1 comanda livrata.",
            _ => $"{orders.Count} comenzi livrate.",
        };

        _text.Clear();
        foreach (var order in orders)
        {
            AppendOrder(order);
        }

        var pending = Services.Orders.GetPendingRatings().Count;
        _rateButton.Text = pending > 0 ? $"Evalueaza produsele ({pending})" : "Toate produsele sunt evaluate";
        _rateButton.Enabled = pending > 0;
    }

    private void AppendOrder(Order order)
    {
        Write($"Data livrarii: {order.DeliveryDate:dd-MM-yyyy}", bold: true);
        Write($"Comanda {order.ShortId}, plasata la {order.OrderDate:dd-MM-yyyy}", muted: true);

        for (var index = 0; index < order.Items.Count; index++)
        {
            var item = order.Items[index];
            var stars = item.Rating is { } rating ? $"   {new string('★', rating)}{new string('☆', 5 - rating)}" : string.Empty;
            Write($"Produs {index + 1}: {item.ProductName} / Pret: {item.UnitPrice:N2} lei / Cantitate: {item.Quantity} / Total: {item.Total:N2} lei{stars}");
        }

        if (order.DiscountAmount > 0)
        {
            Write($"Reducere cupon ({order.CouponCode}): -{order.DiscountAmount:N2} lei");
        }

        if (order.SubscriptionDiscount > 0)
        {
            Write($"Reducere abonamente: -{order.SubscriptionDiscount:N2} lei");
        }

        Write($"Costul total al comenzii: {order.TotalCost:N2} lei", bold: true);
        Write(string.Empty);
    }

    private void Write(string line, bool bold = false, bool muted = false)
    {
        _text.SelectionStart = _text.TextLength;
        _text.SelectionFont = bold ? Ui.BoldFont : _text.Font;
        _text.SelectionColor = muted ? Theme.Muted : Theme.Ink;
        _text.AppendText(line + Environment.NewLine);
    }

    private void Rate()
    {
        using var dialog = new RatingDialog(Services);
        dialog.ShowDialog(this);
        Reload();
    }
}
