using MetroREX.Models;
using MetroREX.Services;
 
namespace MetroREX.Forms;
 
/// <summary>Produsele unei categorii, cu sortare după preț și adăugare în coș.</summary>
internal sealed class ProductListView : ViewBase
{
    private readonly string _category;
    private readonly DataGridView _grid;
    private readonly Label _count;
    private readonly Label _message;
    private PriceSort _sort;
 
    public ProductListView(ShopForm shell, string category, PriceSort sort = PriceSort.None) : base(shell)
    {
        _category = category ?? throw new ArgumentNullException(nameof(category));
        _sort = sort;
 
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, _category));
 
        _count = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _count);
 
        var sortBar = Ui.Bar(this);
        sortBar.Controls.Add(Ui.SecondaryButton(this, "Preț crescător", (_, _) => SetSort(PriceSort.Ascending)));
        sortBar.Controls.Add(Ui.SecondaryButton(this, "Preț descrescător", (_, _) => SetSort(PriceSort.Descending)));
        sortBar.Controls.Add(Ui.SecondaryButton(this, "Alfabetic", (_, _) => SetSort(PriceSort.None)));
        Ui.AddRow(column, sortBar);
 
        _message = Ui.Message(this);
        Ui.AddRow(column, _message);
 
        _grid = Ui.Grid(this,
            ("Produs", 45f, false),
            ("Preț", 15f, true),
            ("Stoc", 12f, true),
            ("Evaluare", 28f, false));
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                AddSelectedToCart();
            }
        };
        Ui.AddRow(column, _grid, fill: true);
 
        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.PrimaryButton(this, "Adaugă în coș", (_, _) => AddSelectedToCart()));
        Ui.AddRow(column, actions);
 
        Controls.Add(column);
        Reload();
    }
 
    private void SetSort(PriceSort sort)
    {
        _sort = sort;
        Reload();
    }
 
    private void Reload()
    {
        // Dacă utilizatorul apasă „Înapoi”, ecranul revine cu aceeași sortare.
        var sort = _sort;
        Shell.SetCurrentFactory(() => new ProductListView(Shell, _category, sort));
 
        var products = Services.Catalog.GetProducts(_category, _sort);
        _count.Text = Ui.ProductCount(products.Count);
 
        _grid.Rows.Clear();
        foreach (var product in products)
        {
            var index = _grid.Rows.Add(
                product.Name,
                $"{product.CurrentPrice:N2} lei",
                product.InStock ? product.Quantity.ToString() : "Epuizat",
                Ui.RatingText(product));
            _grid.Rows[index].Tag = product;
        }
 
        Ui.Note(_message, "Dublu-click pe un produs sau „Adaugă în coș” pentru a-l adăuga.");
    }
 
    private void AddSelectedToCart()
    {
        if (_grid.CurrentRow?.Tag is not Product product)
        {
            Ui.Say(_message, "Selectează mai întâi un produs.", isError: true);
            return;
        }
 
        if (Services.Cart.AvailableQuantity(product) < 1)
        {
            Ui.Say(_message, "Nu mai există stoc disponibil pentru acest produs.", isError: true);
            return;
        }
 
        Services.Cart.Add(product.Id, 1);
        Shell.RefreshCartCount();
        Ui.Say(_message, $"„{product.Name}” a fost adăugat în coș.");
    }
}