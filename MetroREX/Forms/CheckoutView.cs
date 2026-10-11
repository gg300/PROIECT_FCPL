using MetroREX.Models;
using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>
/// Finalizarea comenzii: rezumat cu reduceri (stanga) si cupon, date de livrare pentru vizitatori, plata
/// (dreapta). Preturile si reducerile vin din <see cref="CheckoutService.Quote"/>, comanda se plaseaza cu
/// <see cref="CheckoutService.PlaceOrderAsync"/>; formularul doar aduna datele si evidentiaza campul gresit.
/// </summary>
internal sealed class CheckoutView : ViewBase
{
    private readonly DataGridView _grid;
    private readonly Label _breakdown;
    private readonly Label _total;
    private readonly Label _message;
    private readonly ComboBox? _coupon;
    private readonly ProfileFields? _profile;
    private readonly ComboBox _payment;
    private readonly TableLayoutPanel _cardFields;
    private readonly TextBox _holder;
    private readonly TextBox _number;
    private readonly TextBox _expiry;
    private readonly TextBox _cvv;

    public CheckoutView(ShopForm shell) : base(shell)
    {
        _message = Ui.Message(this);
        _grid = Ui.Grid(this,
            ("Produs", 46f, false),
            ("Pret", 18f, true),
            ("Cant.", 12f, true),
            ("Total", 24f, true));
        _breakdown = Ui.Muted(this, string.Empty);
        _total = Ui.Section(this, string.Empty);

        var form = Ui.Fields(this);

        if (Services.Session.CurrentUser is { } user)
        {
            Ui.AddSection(this, form, "Cupon");
            _coupon = AddCouponPicker(form);

            Ui.AddSection(this, form, "Livrare");
            Ui.AddField(this, form, "Catre", new Label
            {
                Text = $"{user.Profile.Name}, {user.Profile.Address}",
                AutoSize = true,
                ForeColor = Theme.Muted,
            });
        }
        else
        {
            Ui.AddSection(this, form, "Date de livrare");
            _profile = new ProfileFields(this, form);
        }

        Ui.AddSection(this, form, "Plata");
        _payment = Inputs.Choice();
        _payment.Items.Add(new PaymentChoice(PaymentMethod.Cash, "Numerar la livrare"));
        _payment.Items.Add(new PaymentChoice(PaymentMethod.Card, "Card"));
        _payment.SelectedIndex = 0;
        Ui.AddField(this, form, "Metoda", _payment);

        _cardFields = Ui.Fields(this);
        Ui.AddSection(this, _cardFields, "Date card");
        _holder = Ui.AddField(this, _cardFields, "Titular", Inputs.Text());
        _number = Ui.AddField(this, _cardFields, "Numar card", Inputs.Text("1234 5678 9012 3456"));
        _expiry = Ui.AddField(this, _cardFields, "Expira", Inputs.Text("LL/AA"));
        _cvv = Ui.AddField(this, _cardFields, "CVV", Inputs.Password());
        _cardFields.Visible = false;

        // Campurile cardului apar doar pentru plata cu cardul.
        _payment.SelectedIndexChanged += (_, _) => _cardFields.Visible = SelectedPayment == PaymentMethod.Card;

        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Finalizare comanda"));
        Ui.AddRow(column, _message);
        Ui.AddRow(column, BuildBody(form), fill: true);

        var actions = Ui.Bar(this);
        actions.Controls.Add(Ui.SecondaryButton(this, "← Inapoi la cos", (_, _) => Shell.Back()));
        actions.Controls.Add(Ui.PrimaryButton(this, "Plaseaza comanda", async (_, _) => await PlaceOrderAsync()));
        Ui.AddRow(column, actions);

        Controls.Add(column);
        ShowQuote();
    }

    private PaymentMethod SelectedPayment =>
        (_payment.SelectedItem as PaymentChoice)?.Method ?? PaymentMethod.Cash;

    private Coupon? SelectedCoupon => (_coupon?.SelectedItem as CouponChoice)?.Coupon;

    // ===== Constructie =====

    /// <summary>Doar cupoanele valabile pentru o categorie din cos; cosul nu se schimba cat timp ecranul e deschis.</summary>
    private ComboBox AddCouponPicker(TableLayoutPanel form)
    {
        var picker = Inputs.Choice();
        picker.Items.Add(new CouponChoice(null));

        var categories = Services.Cart.Current.Items.Select(item => item.Category).ToList();
        foreach (var coupon in Services.Coupons.GetCoupons())
        {
            if (categories.Contains(coupon.Category, StringComparer.OrdinalIgnoreCase))
            {
                picker.Items.Add(new CouponChoice(coupon));
            }
        }

        picker.SelectedIndex = 0;
        picker.SelectedIndexChanged += (_, _) => ShowQuote();
        return Ui.AddField(this, form, "Cupon", picker);
    }

    private Control BuildBody(TableLayoutPanel form)
    {
        var gap = LogicalToDeviceUnits(24);

        var summary = NewStack(new Padding(0, 0, gap, 0));
        Ui.AddRow(summary, Ui.Section(this, "Rezumatul comenzii"));
        Ui.AddRow(summary, _grid, fill: true);
        Ui.AddRow(summary, _breakdown);
        Ui.AddRow(summary, _total);

        // Formularul poate fi mai inalt decat fereastra (cu campurile cardului), deci se deruleaza.
        var details = NewStack(Padding.Empty);
        details.AutoScroll = true;
        Ui.AddRow(details, form);
        Ui.AddRow(details, _cardFields);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(summary, 0, 0);
        body.Controls.Add(details, 1, 0);
        return body;
    }

    private static TableLayoutPanel NewStack(Padding margin)
    {
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = margin,
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return stack;
    }

    // ===== Rezumat =====

    private void ShowQuote()
    {
        var result = Services.Checkout.Quote(SelectedCoupon?.Code);

        _grid.Rows.Clear();
        if (!result.IsSuccess)
        {
            _breakdown.Text = string.Empty;
            _total.Text = string.Empty;
            Ui.Say(_message, result.Error, isError: true);
            return;
        }

        var quote = result.Value;
        foreach (var item in quote.Items)
        {
            _grid.Rows.Add(
                item.ProductName,
                $"{item.UnitPrice:N2} lei",
                item.Quantity.ToString(),
                $"{item.Total:N2} lei");
        }

        var lines = new List<string> { $"Subtotal: {quote.Subtotal:N2} lei" };
        if (quote.Discount > 0)
        {
            lines.Add($"Reducere cupon: -{quote.Discount:N2} lei");
        }

        if (quote.SubscriptionDiscount > 0)
        {
            lines.Add($"Reducere abonamente: -{quote.SubscriptionDiscount:N2} lei");
        }

        _breakdown.Text = string.Join(Environment.NewLine, lines);
        _total.Text = $"Total de plata: {quote.Total:N2} lei";
        Ui.Note(_message, string.Empty);
    }

    // ===== Plasarea comenzii =====

    private async Task PlaceOrderAsync()
    {
        Ui.Note(_message, string.Empty);

        var request = TryBuildRequest();
        if (request is null)
        {
            return;
        }

        Shell.SetBusy(true);
        CheckoutOutcome? outcome;
        string? error;
        try
        {
            (outcome, error) = await SubmitAsync(request);
        }
        finally
        {
            Shell.SetBusy(false);
        }

        if (outcome is null)
        {
            Ui.Say(_message, error ?? "Comanda nu a putut fi plasata.", isError: true);
            return;
        }

        Sfx.Play(Sound.OrderPlaced);
        MessageBox.Show(FindForm(), Describe(outcome), "Comanda plasata", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Shell.GoHome();
    }

    private async Task<(CheckoutOutcome? Outcome, string? Error)> SubmitAsync(CheckoutRequest request)
    {
        try
        {
            var result = await Services.Checkout.PlaceOrderAsync(request);
            return result.IsSuccess ? (result.Value, null) : (null, result.Error);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return (null, $"Datele nu au putut fi salvate: {exception.Message}");
        }
    }

    /// <summary>Strange datele din formular; verificarile de aici doar evidentiaza campul gresit, restul il decide serviciul.</summary>
    private CheckoutRequest? TryBuildRequest()
    {
        UserProfile? guestProfile = null;
        if (_profile is not null)
        {
            if (_profile.FindProblem() is { } problem)
            {
                Fail(problem.Message, problem.Field);
                return null;
            }

            guestProfile = _profile.ToProfile();
        }

        CardDetails? card = null;
        if (SelectedPayment == PaymentMethod.Card)
        {
            if (!Validation.TryParseExpiry(_expiry.Text, out var month, out var year))
            {
                Fail("Data de expirare trebuie sa fie in forma LL/AA (de exemplu 08/28).", _expiry);
                return null;
            }

            card = new CardDetails
            {
                Holder = _holder.Text,
                Number = _number.Text,
                ExpiryMonth = month,
                ExpiryYear = year,
                Cvv = _cvv.Text,
            };
        }

        return new CheckoutRequest
        {
            GuestProfile = guestProfile,
            PaymentMethod = SelectedPayment,
            Card = card,
            CouponCode = SelectedCoupon?.Code,
        };
    }

    private static string Describe(CheckoutOutcome outcome)
    {
        var order = outcome.Order;
        var emailNote = outcome.EmailSent
            ? $"Confirmarea a fost trimisa la {order.Profile.Email}."
            : $"Comanda a fost plasata, dar e-mailul de confirmare nu a putut fi trimis: {outcome.EmailError}";

        return $"Comanda {order.ShortId} a fost plasata.{Environment.NewLine}" +
               $"Total: {order.TotalCost:N2} lei{Environment.NewLine}" +
               $"Livrare estimata: {order.DeliveryDate:dd-MM-yyyy}{Environment.NewLine}{Environment.NewLine}" +
               emailNote;
    }

    private void Fail(string error, Control focus)
    {
        Ui.Say(_message, error, isError: true);
        focus.Focus();

        if (focus is TextBox box)
        {
            box.SelectAll();
        }
    }

    // ===== Elemente de lista =====

    private sealed record PaymentChoice(PaymentMethod Method, string Text)
    {
        public override string ToString() => Text;
    }

    private sealed record CouponChoice(Coupon? Coupon)
    {
        public override string ToString() => Coupon is null ? "Fara cupon" : $"{Coupon.Code}: {Coupon.Details}";
    }
}
