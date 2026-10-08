using MetroREX.Controls;
using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>Fereastra principala: Login / Creare cont / Fara cont.</summary>
internal sealed class MainForm : Form
{
    private readonly ShopServices _services;

    public MainForm(ShopServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Text = "MetroREX";
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = LogicalToDeviceUnits(new Size(1200, 720));
        MinimumSize = LogicalToDeviceUnits(new Size(1000, 640));
        DoubleBuffered = true;

        var body = BuildBody();
        Controls.Add(body);
        Controls.Add(BuildHeader());
        Controls.Add(BuildFooter());
        body.BringToFront(); // corpul ocupa spatiul ramas intre header si footer
    }

    // ===== Header: numele magazinului =====
    private Control BuildHeader()
    {
        var header = new LinePanel
        {
            Dock = DockStyle.Top,
            Height = LogicalToDeviceUnits(64),
            Padding = new Padding(LogicalToDeviceUnits(48), 0, LogicalToDeviceUnits(48), LogicalToDeviceUnits(2)),
        };

        header.Controls.Add(new Label
        {
            Text = "Magazin online de haine",
            Dock = DockStyle.Right,
            Width = LogicalToDeviceUnits(260),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Muted,
        });
        header.Controls.Add(new Label
        {
            Text = "MetroREX",
            Dock = DockStyle.Left,
            Width = LogicalToDeviceUnits(240),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Wordmark,
            ForeColor = Theme.Ink,
        });
        return header;
    }

    // ===== Corpul: logo + titlu in stanga, meniul in dreapta =====
    private Control BuildBody()
    {
        int pad = LogicalToDeviceUnits(48);
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(pad, pad / 2, pad, pad / 2),
            BackColor = Theme.Concrete,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var hero = new HeroPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, pad / 2, 0),
        };

        var menu = new StationLine
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(pad / 2, 0, 0, 0),
        };

        var login = menu.AddStation("Autentificare",
            "Ai deja cont? Intra ca sa vezi comenzile si lista de dorinte.");
        var register = menu.AddStation("Creeaza cont",
            "Primesti istoric de comenzi, comenzi active si wish list.");
        var guest = menu.AddStation("Intra fara cont",
            "Rasfoiesti si cumperi produse, fara wish list si istoric.", isGuest: true);

        login.Click += (_, _) => Authenticate(() => new LoginForm(_services));
        register.Click += (_, _) => Authenticate(() => new RegisterForm(_services));
        guest.Click += (_, _) => OpenShop();

        table.Controls.Add(hero, 0, 0);
        table.Controls.Add(menu, 1, 0);
        return table;
    }

    // ===== Footer =====
    private Control BuildFooter()
    {
        var footer = new LinePanel
        {
            Dock = DockStyle.Bottom,
            Height = LogicalToDeviceUnits(48),
            LineOnTop = true,
            Padding = new Padding(LogicalToDeviceUnits(48), LogicalToDeviceUnits(2), LogicalToDeviceUnits(48), 0),
        };
        footer.Controls.Add(new Label
        {
            Text = "© 2026 MetroREX, un proiect de Timotei si George",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
        });
        return footer;
    }

    // ===== Navigare =====

    /// <summary>Afiseaza fereastra de autentificare; daca utilizatorul a intrat in cont, deschide magazinul.</summary>
    private void Authenticate(Func<AuthFormBase> createDialog)
    {
        using var dialog = createDialog();

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            OpenShop();
        }
    }

    /// <summary>Deschide magazinul (cu utilizatorul curent din sesiune sau ca vizitator) si ascunde fereastra principala.</summary>
    private void OpenShop()
    {
        var shopForm = new ShopForm(_services);

        shopForm.FormClosed += (_, _) =>
        {
            // Inchiderea magazinului incheie sesiunea (si goleste cosul), ca urmatoarea alegere din meniu
            // sa porneasca de la zero.
            _services.Auth.Logout();
            Show();
        };

        Hide();
        shopForm.Show();
    }
}
