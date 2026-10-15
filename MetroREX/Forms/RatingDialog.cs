using MetroREX.Controls;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class RatingDialog : Form
{
    private readonly ShopServices _services;
    private readonly Label _progress;
    private readonly Label _order;
    private readonly Label _product;
    private readonly Label _message;

    private PendingRating? _current;
    private int _done;
    private bool _closeWhenShown;

    public RatingDialog(ShopServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));

        Text = "MetroREX – Evalueaza produsele";
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var width = LogicalToDeviceUnits(460);
        var inset = LogicalToDeviceUnits(32);

        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(inset, inset / 2, inset, inset / 2),
            MinimumSize = new Size(width + 2 * inset, 0),
            BackColor = Theme.Concrete,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _progress = Ui.Muted(this, string.Empty);
        _order = Ui.Muted(this, string.Empty);
        _product = new Label
        {
            AutoSize = true,
            Font = new Font(Theme.FontName, 14f, FontStyle.Bold),
            ForeColor = Theme.Ink,
            MaximumSize = new Size(width, 0),
            Margin = new Padding(0, LogicalToDeviceUnits(4), 0, LogicalToDeviceUnits(12)),
        };
        _message = Ui.Message(this);

        var stars = new StarRating(LogicalToDeviceUnits(40))
        {
            Margin = new Padding(0, LogicalToDeviceUnits(4), 0, LogicalToDeviceUnits(8)),
        };
        stars.Rated += (_, value) => Submit(value);

        var later = Ui.Bar(this);
        later.Controls.Add(Ui.SecondaryButton(this, "Mai tarziu", (_, _) => Close()));

        Ui.AddRow(root, Ui.Title(this, "Cum ti s-a parut?"));
        Ui.AddRow(root, _progress);
        Ui.AddRow(root, _order);
        Ui.AddRow(root, _product);
        Ui.AddRow(root, Ui.Muted(this, "Alege o nota de la 1 la 5 stele:"));
        Ui.AddRow(root, stars);
        Ui.AddRow(root, _message);
        Ui.AddRow(root, later);
        Controls.Add(root);

        CancelButton = (IButtonControl)later.Controls[0];
        ShowNext();
    }

    public static bool HasPending(ShopServices services) =>
        services.Session.IsLoggedIn && services.Orders.GetPendingRatings().Count > 0;

    private void ShowNext()
    {
        var pending = _services.Orders.GetPendingRatings();
        if (pending.Count == 0)
        {
            DialogResult = DialogResult.OK;
            if (IsHandleCreated)
            {
                Close();
            }
            else
            {
                _closeWhenShown = true;
            }

            return;
        }

        _current = pending[0];
        _progress.Text = $"Evaluate: {_done}   |   Ramase: {pending.Count}";
        _order.Text = $"Comanda {_current.Order.ShortId}, livrata la {_current.Order.DeliveryDate:dd-MM-yyyy}";
        _product.Text = _current.Item.ProductName;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_closeWhenShown)
        {
            Close();
        }
    }

    private void Submit(int stars)
    {
        if (_current is null)
        {
            return;
        }

        var result = _services.Orders.Rate(_current.Order.Id, _current.Item.ProductId, stars);
        if (!result.IsSuccess)
        {
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        Sfx.Play(Sound.Tick);
        _done++;
        Ui.Note(_message, string.Empty);
        ShowNext();
    }
}
