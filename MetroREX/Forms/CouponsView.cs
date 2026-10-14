using MetroREX.Models;

namespace MetroREX.Forms;

internal sealed class CouponsView : ViewBase, ILiveView
{
    private readonly DataGridView _grid;
    private readonly TextBox _code;
    private readonly Label _message;
    private readonly Label _count;

    public CouponsView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Cupoanele mele"));

        _count = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _count);

        var add = Ui.Bar(this);
        add.Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(4));
        _code = Inputs.Text("AAA-BBB-CCC");
        _code.CharacterCasing = CharacterCasing.Upper;
        _code.MaxLength = 11;
        _code.Width = LogicalToDeviceUnits(180);
        _code.Margin = new Padding(0, LogicalToDeviceUnits(8), LogicalToDeviceUnits(10), 0);
        _code.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Redeem();
            }
        };
        add.Controls.Add(new Label
        {
            Text = "Cupon nou:",
            AutoSize = true,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, LogicalToDeviceUnits(11), LogicalToDeviceUnits(8), 0),
        });
        add.Controls.Add(_code);
        add.Controls.Add(Ui.PrimaryButton(this, "Adauga cuponul", (_, _) => Redeem()));
        Ui.AddRow(column, add);

        _message = Ui.Message(this);
        Ui.AddRow(column, _message);

        _grid = Ui.Grid(this,
            ("Cod", 22f, false),
            ("Categorie", 30f, false),
            ("Expira la", 18f, false),
            ("Detalii", 30f, false));
        Ui.AddRow(column, _grid, fill: true);

        Ui.AddRow(column, Ui.Muted(this,
            "Un cupon se foloseste o singura data, la finalizarea comenzii, pentru produsele din categoria lui."));

        Controls.Add(column);
        Reload();
    }

    private void Reload()
    {
        var coupons = Services.Coupons.GetCoupons();

        _grid.Rows.Clear();
        foreach (var coupon in coupons)
        {
            _grid.Rows.Add(
                coupon.Code,
                coupon.Category,
                coupon.ExpirationDate.ToString("dd-MM-yyyy"),
                $"-{coupon.DiscountPercent}% la produsele din aceasta categorie");
        }

        _count.Text = coupons.Count switch
        {
            0 => "Nu ai cupoane valabile.",
            1 => "1 cupon valabil.",
            _ => $"{coupons.Count} cupoane valabile.",
        };
    }

    private void Redeem()
    {
        if (!Coupon.IsValidFormat(_code.Text))
        {
            Ui.Say(_message, "Codul trebuie sa aiba forma AAA-BBB-CCC (litere si cifre).", isError: true);
            _code.Focus();
            return;
        }

        var result = Services.Coupons.Redeem(_code.Text);
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            _code.Focus();
            _code.SelectAll();
            return;
        }

        Sfx.Play(Sound.Tick);
        _code.Clear();
        Reload();
        Ui.Say(_message, $"Cuponul {result.Value.Code} a fost adaugat: {result.Value.Details}.");
    }
}
