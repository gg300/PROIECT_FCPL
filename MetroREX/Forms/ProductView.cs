using MetroREX.Controls;
using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class ProductView : ViewBase
{
    private readonly string _productId;
    private readonly Label _message;
    private readonly NumericUpDown _quantity;
    private readonly Label _stockNote;
    private readonly Button? _wishButton;

    public ProductView(ShopForm shell, string productId) : base(shell)
    {
        _productId = productId ?? throw new ArgumentNullException(nameof(productId));

        var column = Ui.Column(this);
        _message = Ui.Message(this);
        _quantity = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 1,
            Value = 1,
            Width = LogicalToDeviceUnits(80),
            Font = new Font(Theme.FontName, 11f),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0, LogicalToDeviceUnits(8), LogicalToDeviceUnits(10), 0),
        };
        _stockNote = Ui.Muted(this, string.Empty);
        _stockNote.Margin = new Padding(0, LogicalToDeviceUnits(12), LogicalToDeviceUnits(16), 0);

        var product = Services.Catalog.FindProduct(_productId);
        if (product is null)
        {
            Ui.AddRow(column, Ui.Title(this, "Produs indisponibil"));
            Ui.AddRow(column, Ui.Muted(this, "Produsul nu mai exista in magazin."));
            var back = Ui.Bar(this);
            back.Controls.Add(Ui.SecondaryButton(this, "← Inapoi", (_, _) => Shell.Back()));
            Ui.AddRow(column, back);
            Controls.Add(column);
            return;
        }

        Ui.AddRow(column, Ui.Title(this, product.Name));
        Ui.AddRow(column, Ui.Muted(this, $"Categoria: {product.Category}"));
        Ui.AddRow(column, _message);
        Ui.AddRow(column, BuildBody(product), fill: true);

        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.SecondaryButton(this, "← Inapoi", (_, _) => Shell.Back()));
        actions.Controls.Add(new Label
        {
            Text = "Cantitate:",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Margin = new Padding(LogicalToDeviceUnits(16), LogicalToDeviceUnits(12), LogicalToDeviceUnits(6), 0),
        });
        actions.Controls.Add(_quantity);
        actions.Controls.Add(_stockNote);
        actions.Controls.Add(Ui.PrimaryButton(this, "Adauga in cos", (_, _) => AddToCart()));

        if (Services.Session.IsLoggedIn)
        {
            _wishButton = Ui.SecondaryButton(this, string.Empty, (_, _) => ToggleWishList());
            actions.Controls.Add(_wishButton);
        }

        Ui.AddRow(column, actions);
        Controls.Add(column);

        RefreshStock();
        RefreshWishButton();
    }

    private Control BuildBody(Product product)
    {
        var gap = LogicalToDeviceUnits(24);

        var details = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, gap, 0),
        };

        var fields = Ui.Fields(this);
        Ui.AddSection(this, fields, "Detalii");
        Ui.AddField(this, fields, "Pret", new Label
        {
            Text = PriceText(product),
            AutoSize = true,
            Font = Ui.BoldFont,
            ForeColor = Theme.Ink,
        });
        Ui.AddField(this, fields, "Stoc disponibil", Value(product.InStock ? $"{product.Quantity} bucati" : "Epuizat"));
        Ui.AddField(this, fields, "Evaluare", RatingDisplay(product));
        Ui.AddField(this, fields, "Vanzator", Value(string.IsNullOrWhiteSpace(product.Seller) ? "-" : product.Seller));

        if (product.Colors.Count > 0)
        {
            Ui.AddField(this, fields, "Culori disponibile", Value(string.Join(", ", product.Colors)));
        }

        Ui.AddSection(this, fields, "Descriere");
        var description = new Label
        {
            Text = string.IsNullOrWhiteSpace(product.Description) ? "Fara descriere." : product.Description,
            AutoSize = true,
            ForeColor = Theme.Ink,
            MaximumSize = new Size(LogicalToDeviceUnits(460), 0),
            Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(8)),
        };
        var row = fields.RowStyles.Count;
        fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        fields.Controls.Add(description, 0, row);
        fields.SetColumnSpan(description, 2);

        details.Controls.Add(fields);

        var specs = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        specs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Ui.AddRow(specs, Ui.Section(this, "Specificatii"));

        var grid = Ui.Grid(this, ("Specificatie", 40f, false), ("Valoare", 60f, false));
        foreach (var spec in product.Specs)
        {
            grid.Rows.Add(spec.Name, spec.Value);
        }

        if (product.Specs.Count == 0)
        {
            grid.Rows.Add("-", "Produsul nu are specificatii.");
        }

        Ui.AddRow(specs, grid, fill: true);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(details, 0, 0);
        body.Controls.Add(specs, 1, 0);
        return body;
    }

    private static Label Value(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Ink,
    };

    private string PriceText(Product product)
    {
        var text = $"{product.CurrentPrice:N2} lei";
        if (Services.Subscriptions.IsSubscribed(product.Seller))
        {
            text += $"   (abonament: -{SubscriptionService.DiscountPercent}% la finalizare)";
        }

        return text;
    }

    private Control RatingDisplay(Product product)
    {
        var flow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };

        if (product.RatingCount > 0)
        {
            var full = (int)Math.Round(product.Rating, MidpointRounding.AwayFromZero);
            flow.Controls.Add(new StarRating(LogicalToDeviceUnits(18))
            {
                Value = full,
                ReadOnly = true,
                Margin = new Padding(0, LogicalToDeviceUnits(2), LogicalToDeviceUnits(8), 0),
            });
        }

        flow.Controls.Add(new Label
        {
            Text = Ui.RatingText(product),
            AutoSize = true,
            ForeColor = Theme.Ink,
            Margin = new Padding(0, LogicalToDeviceUnits(3), 0, 0),
        });
        return flow;
    }

    private void RefreshStock()
    {
        var product = Services.Catalog.FindProduct(_productId);
        var available = product is null ? 0 : Services.Cart.AvailableQuantity(product);

        _quantity.Maximum = Math.Max(1, available);
        _quantity.Enabled = available > 0;
        _stockNote.Text = available > 0 ? $"(max. {available})" : "(stoc epuizat)";
    }

    private void RefreshWishButton()
    {
        if (_wishButton is not null)
        {
            _wishButton.Text = Services.WishList.Contains(_productId) ? "Scoate din wish list" : "Adauga in wish list";
        }
    }

    private void AddToCart()
    {
        var quantity = (int)_quantity.Value;
        var result = Services.Cart.Add(_productId, quantity);

        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            RefreshStock();
            return;
        }

        Sfx.Play(Sound.AddToCart);
        Shell.RefreshCartCount();
        Ui.Say(_message, quantity == 1
            ? "Produsul a fost adaugat in cos."
            : $"Au fost adaugate {quantity} bucati in cos.");
        _quantity.Value = 1;
        RefreshStock();
    }

    private void ToggleWishList()
    {
        var wasInList = Services.WishList.Contains(_productId);
        var result = wasInList ? Services.WishList.Remove(_productId) : Services.WishList.Add(_productId);

        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Sfx.Play(Sound.Tick);
        Ui.Say(_message, wasInList
            ? "Produsul a fost scos din wish list."
            : "Produsul a fost adaugat in wish list. Vei primi notificari cand i se schimba pretul.");
        RefreshWishButton();
    }
}
