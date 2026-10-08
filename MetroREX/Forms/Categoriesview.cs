namespace MetroREX.Forms;
 
/// <summary>Ecranul de start al magazinului: categoriile, ca butoane mari cu numarul de produse.</summary>
internal sealed class CategoriesView : ViewBase
{
    public CategoriesView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Categorii"));
        Ui.AddRow(column, Ui.Muted(this, "Alege o categorie ca sa vezi produsele."));
 
        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
 
        var categories = Services.Catalog.GetCategories();
        if (categories.Count == 0)
        {
            cards.Controls.Add(Ui.Muted(this, "Momentan nu exista produse in magazin."));
        }
 
        foreach (var category in categories)
        {
            var count = Services.Catalog.GetProducts(category).Count;
            var card = Ui.SecondaryButton(this, $"{category}{Environment.NewLine}{Ui.ProductCount(count)}",
                (_, _) => Shell.Navigate(() => new ProductListView(Shell, category)));
 
            card.AutoSize = false;
            card.Size = new Size(LogicalToDeviceUnits(260), LogicalToDeviceUnits(110));
            card.Font = Ui.BoldFont;
            card.Margin = new Padding(0, 0, LogicalToDeviceUnits(16), LogicalToDeviceUnits(16));
            cards.Controls.Add(card);
        }
 
        Ui.AddRow(column, cards, fill: true);
        Controls.Add(column);
    }
}