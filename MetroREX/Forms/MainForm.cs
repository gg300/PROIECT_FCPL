using MetroREX.Controls;
using MetroREX.Services;
 
namespace MetroREX.Forms;
 
/// <summary>Fereastra principală: Login / Creare cont / Fără cont.</summary>
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
        body.BringToFront(); // corpul ocupă spațiul rămas între header și footer
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
 
    // ===== Corpul: logo + titlu în stânga, meniul în dreapta =====
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
            "Ai deja cont? Intră ca să vezi comenzile și lista de dorințe.");
        var register = menu.AddStation("Creează cont",
            "Primești istoric de comenzi, comenzi active și wish list.");
        var guest = menu.AddStation("Intră fără cont",
            "Răsfoiești și cumperi produse, fără wish list și istoric.", isGuest: true);
 
        // TODO: înlocuiește cu ferestrele reale când le facem (LoginForm, RegisterForm)
        login.Click    += (_, _) => ComingSoon("Autentificare");
        register.Click += (_, _) => ComingSoon("Creează cont");
 
        // Deschidem ShopForm și ascundem fereastra principală temporar
        guest.Click += (_, _) =>
        {
            var shopForm = new ShopForm(_services);
            this.Hide();
 
            shopForm.FormClosed += (s, args) => this.Show();
            shopForm.Show();
        };
 
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
            Text = "© 2026 MetroREX, un proiect de Timotei și George",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
        });
        return footer;
    }
 
    private void ComingSoon(string screen) =>
        MessageBox.Show(this, $"Ecranul „{screen}” urmează să fie construit.", "MetroREX",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
}