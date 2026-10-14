using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class SubscriptionsView : ViewBase
{
    private readonly DataGridView _grid;
    private readonly Label _summary;
    private readonly Label _message;

    public SubscriptionsView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Abonamente"));
        Ui.AddRow(column, Ui.Muted(this,
            $"Abonat la un vanzator, primesti automat {SubscriptionService.DiscountPercent}% reducere la produsele lui, " +
            $"la finalizarea comenzii. Poti avea cel mult {SubscriptionService.MaxSubscriptions} abonamente."));

        _summary = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _summary);

        _message = Ui.Message(this);
        Ui.AddRow(column, _message);

        _grid = Ui.Grid(this,
            ("Vanzator", 40f, false),
            ("Produse", 20f, true),
            ("Abonament", 40f, false));
        Ui.AddRow(column, _grid, fill: true);

        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.PrimaryButton(this, "Aboneaza-te",
            (_, _) => ChangeSelected(Services.Subscriptions.Subscribe, "Te-ai abonat la „{0}”.")));
        actions.Controls.Add(Ui.SecondaryButton(this, "Renunta la abonament",
            (_, _) => ChangeSelected(Services.Subscriptions.Unsubscribe, "Ai renuntat la abonamentul „{0}”.")));
        Ui.AddRow(column, actions);

        Controls.Add(column);
        Reload();
    }

    private void Reload(string? selectedSeller = null)
    {
        var offers = Services.Subscriptions.GetSellers();

        _grid.Rows.Clear();
        foreach (var offer in offers)
        {
            var index = _grid.Rows.Add(
                offer.Name,
                Ui.ProductCount(offer.ProductCount),
                offer.IsSubscribed ? $"Abonat (-{SubscriptionService.DiscountPercent}%)" : "—");
            _grid.Rows[index].Tag = offer.Name;

            if (string.Equals(offer.Name, selectedSeller, StringComparison.OrdinalIgnoreCase))
            {
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
            }
        }

        var active = offers.Count(offer => offer.IsSubscribed);
        Ui.Note(_summary, $"Abonamente active: {active} din {SubscriptionService.MaxSubscriptions}.");
    }

    private void ChangeSelected(Func<string, Result> change, string successFormat)
    {
        if (_grid.CurrentRow?.Tag is not string seller)
        {
            Ui.Say(_message, "Selecteaza mai intai un vanzator.", isError: true);
            return;
        }

        var result = change(seller);
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Reload(seller);
        Ui.Say(_message, string.Format(successFormat, seller));
    }
}
