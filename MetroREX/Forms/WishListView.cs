using MetroREX.Models;

namespace MetroREX.Forms;

/// <summary>
/// Wish list-ul utilizatorului: nume, pret, cantitate disponibila. Deschiderea unui produs duce la pagina
/// lui; „Inapoi” de acolo revine aici (nu la lista de produse), pentru ca ecranul acesta ramane in istoric.
/// </summary>
internal sealed class WishListView : ViewBase, ILiveView
{
    private readonly DataGridView _grid;
    private readonly Label _count;
    private readonly Label _message;

    public WishListView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Wish list"));

        _count = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _count);

        _message = Ui.Message(this);
        Ui.AddRow(column, _message);

        _grid = Ui.Grid(this,
            ("Produs", 50f, false),
            ("Pret", 20f, true),
            ("Cantitate disponibila", 30f, true));
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                OpenSelected();
            }
        };
        Ui.AddRow(column, _grid, fill: true);

        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.PrimaryButton(this, "Deschide produsul", (_, _) => OpenSelected()));
        actions.Controls.Add(Ui.SecondaryButton(this, "Scoate din wish list", (_, _) => RemoveSelected()));
        Ui.AddRow(column, actions);

        Controls.Add(column);
        Reload();
    }

    private Product? Selected => _grid.CurrentRow?.Tag as Product;

    private void Reload()
    {
        var items = Services.WishList.GetItems();

        _grid.Rows.Clear();
        foreach (var product in items)
        {
            var index = _grid.Rows.Add(
                product.Name,
                $"{product.CurrentPrice:N2} lei",
                product.InStock ? product.Quantity.ToString() : "Epuizat");
            _grid.Rows[index].Tag = product;
        }

        _count.Text = items.Count == 0
            ? "Wish list-ul este gol. Adauga produse din pagina lor."
            : Ui.ProductCount(items.Count);

        Ui.Note(_message, items.Count == 0 ? string.Empty : "Dublu-click pe un produs ca sa-l deschizi si sa-l comanzi.");
    }

    private void OpenSelected()
    {
        if (Selected is not { } product)
        {
            Ui.Say(_message, "Selecteaza mai intai un produs.", isError: true);
            return;
        }

        var id = product.Id;
        Shell.Navigate(() => new ProductView(Shell, id));
    }

    private void RemoveSelected()
    {
        if (Selected is not { } product)
        {
            Ui.Say(_message, "Selecteaza mai intai un produs.", isError: true);
            return;
        }

        var result = Services.WishList.Remove(product.Id);
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Sfx.Play(Sound.RemoveFromCart);
        Reload();
        Ui.Say(_message, $"„{product.Name}” a fost scos din wish list.");
    }
}
