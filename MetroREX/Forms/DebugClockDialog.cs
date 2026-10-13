using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>
/// Metoda speciala pentru testare (cerinta din proiect): schimba data/ora „magazinului”.
/// Totul in aplicatie foloseste <see cref="Clock"/>, deci o zi noua simulata declanseaza imediat
/// procesele zilnice: variatia preturilor, notificarile, cuponul zilnic si livrarile.
/// Se deschide din magazin cu Ctrl+Shift+T.
/// </summary>
internal sealed class DebugClockDialog : Form
{
    private readonly Clock _clock;
    private readonly DateTimePicker _picker;
    private readonly Label _now;

    public DebugClockDialog(Clock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));

        Text = "MetroREX – Data pentru testare";
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

        var inset = LogicalToDeviceUnits(28);
        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(inset, inset / 2, inset, inset / 2),
            MinimumSize = new Size(LogicalToDeviceUnits(520), 0),
            BackColor = Theme.Concrete,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _now = new Label
        {
            AutoSize = true,
            Font = Ui.BoldFont,
            ForeColor = Theme.Ink,
            Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(12)),
        };

        _picker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd-MM-yyyy HH:mm",
            Width = LogicalToDeviceUnits(200),
            Font = new Font(Theme.FontName, 10.5f),
            Margin = new Padding(0, LogicalToDeviceUnits(8), LogicalToDeviceUnits(10), 0),
        };

        var setRow = Ui.Bar(this);
        setRow.Controls.Add(_picker);
        setRow.Controls.Add(Ui.PrimaryButton(this, "Seteaza data", (_, _) => Apply(() => _clock.SetNow(_picker.Value))));

        var quick = Ui.Bar(this);
        quick.Controls.Add(Ui.SecondaryButton(this, "+1 ora", (_, _) => Apply(() => _clock.Advance(TimeSpan.FromHours(1)))));
        quick.Controls.Add(Ui.SecondaryButton(this, "+1 zi", (_, _) => Apply(() => _clock.AdvanceDays(1))));
        quick.Controls.Add(Ui.SecondaryButton(this, "+3 zile", (_, _) => Apply(() => _clock.AdvanceDays(3))));
        quick.Controls.Add(Ui.SecondaryButton(this, "Ceasul real", (_, _) => Apply(_clock.Reset)));

        var close = Ui.Bar(this);
        var closeButton = Ui.SecondaryButton(this, "Inchide", (_, _) => Close());
        close.Controls.Add(closeButton);

        var hint = Ui.Muted(this,
            "La fiecare zi noua: preturile variaza cu ±20%, se trimit notificarile din wish list, " +
            "se genereaza un cupon nou (Storage/coupons.txt) si se livreaza comenzile scadente.");
        hint.MaximumSize = new Size(LogicalToDeviceUnits(500), 0);

        Ui.AddRow(root, Ui.Title(this, "Data pentru testare"));
        Ui.AddRow(root, _now);
        Ui.AddRow(root, hint);
        Ui.AddRow(root, setRow);
        Ui.AddRow(root, quick);
        Ui.AddRow(root, close);
        Controls.Add(root);

        CancelButton = closeButton;
        ShowNow();
    }

    private void Apply(Action change)
    {
        change();
        Sfx.Play(Sound.Tick);
        ShowNow();
    }

    private void ShowNow()
    {
        var now = _clock.Now;
        _now.Text = _clock.IsOverridden
            ? $"Data simulata: {now:dd-MM-yyyy HH:mm}"
            : $"Data reala: {now:dd-MM-yyyy HH:mm}";
        _picker.Value = now;
    }
}
