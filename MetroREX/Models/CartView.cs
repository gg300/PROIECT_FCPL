using MetroREX.Models;
 
namespace MetroREX.Forms;
 
/// <summary>Coșul de cumpărături: produse, cantități, total.</summary>
internal sealed class CartView : ViewBase
{
    private readonly DataGridView _grid;
    private readonly Label _total;
    private readonly Label _message;
 
    public CartView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Coșul tău"));
 
        _message = Ui.Message(this);
        Ui.AddRow(column, _message);
 
        _grid = Ui.Grid(this,
            ("Produs", 40f, false),
            ("Preț unitar", 18f, true),
            ("Cantitate", 14f, true),
            ("Total", 18f, true));
        Ui.AddRow(column, _grid, fill: true);
 
        _total = Ui.Section(this, string.Empty);
        Ui.AddRow(column, _total);
 
        var bar = Ui.Bar(this);
        bar.Controls.Add(Ui.SecondaryButton(this, "+ 1", (_, _) => ChangeQuantity(+1)));
        bar.Controls.Add(Ui.SecondaryButton(this, "− 1", (_, _) => ChangeQuantity(-1)));
        bar.Controls.Add(Ui.SecondaryButton(this, "Șterge produsul", (_, _) => RemoveSelected()));
        bar.Controls.Add(Ui.SecondaryButton(this, "Golește coșul", (_, _) => ClearCart()));
        bar.Controls.Add(Ui.SecondaryButton(this, "Continuă cumpărăturile", (_, _) => Shell.GoHome()));
        bar.Controls.Add(Ui.PrimaryButton(this, "Finalizează comanda", (_, _) => Checkout()));
        Ui.AddRow(column, bar);
 
        Controls.Add(column);
        Reload(selectProductId: null);
    }
 
    private CartItem? SelectedItem => _grid.CurrentRow?.Tag as CartItem;
 
    private void Reload(string? selectProductId)
    {
        var cart = Services.Cart.Current;
 
        _grid.Rows.Clear();
        foreach (var item in cart.Items)
        {
            var index = _grid.Rows.Add(
                item.ProductName,
                $"{item.UnitPrice:N2} lei",
                item.Quantity.ToString(),
                $"{item.Total:N2} lei");
            _grid.Rows[index].Tag = item;
 
            if (selectProductId is not null && item.ProductId == selectProductId)
            {
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
            }
        }
 
        _total.Text = cart.IsEmpty ? "Coșul este gol." : $"Total: {cart.Subtotal:N2} lei";
        Shell.RefreshCartCount();
    }
 
    private void ChangeQuantity(int delta)
    {
        var item = SelectedItem;
        if (item is null)
        {
            Ui.Say(_message, "Selectează mai întâi un produs din coș.", isError: true);
            return;
        }
 
        var newQuantity = item.Quantity + delta;
        if (newQuantity < 1)
        {
            Ui.Say(_message, "Cantitatea minimă este 1. Folosește „Șterge produsul” ca să îl scoți din coș.", isError: true);
            return;
        }
 
        var product = Services.Catalog.FindProduct(item.ProductId);
        if (product is null)
        {
            Ui.Say(_message, "Produsul nu mai există în magazin.", isError: true);
            return;
        }
 
        if (newQuantity > product.Quantity)
        {
            Ui.Say(_message, $"Sunt disponibile doar {product.Quantity} bucăți.", isError: true);
            return;
        }
 
        var productId = item.ProductId;
        Services.Cart.SetQuantity(productId, newQuantity);
        Ui.Note(_message, string.Empty);
        Reload(productId);
    }
 
    private void RemoveSelected()
    {
        var item = SelectedItem;
        if (item is null)
        {
            Ui.Say(_message, "Selectează mai întâi un produs din coș.", isError: true);
            return;
        }
 
        Services.Cart.Remove(item.ProductId);
        Ui.Say(_message, $"„{item.ProductName}” a fost scos din coș.");
        Reload(selectProductId: null);
    }
 
    private void ClearCart()
    {
        if (Services.Cart.Current.IsEmpty)
        {
            return;
        }
 
        Services.Cart.Clear();
        Ui.Say(_message, "Coșul a fost golit.");
        Reload(selectProductId: null);
    }
 
    private void Checkout()
    {
        if (Services.Cart.Current.IsEmpty)
        {
            Ui.Say(_message, "Coșul este gol.", isError: true);
            return;
        }
 
        // TODO: ecranul de finalizare (adresă, plată, cupon) folosește Services.Checkout.
        Ui.Note(_message, "Ecranul de finalizare a comenzii urmează să fie construit.");
    }
}