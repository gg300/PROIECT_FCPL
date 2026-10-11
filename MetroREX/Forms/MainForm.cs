using MetroREX.Controls;
using MetroREX.Services;

namespace MetroREX.Forms;

/// <summary>Fereastra principala (pagina de start): Login / Creare cont / Fara cont. Layout responsive.</summary>
internal sealed class MainForm : Form
{
    private readonly ShopServices _services;
    private readonly HeaderBar _header;
    private readonly LinePanel _footer;
    private Label _tagline = null!;

    public MainForm(ShopServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        Text = "MetroREX";
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = LogicalToDeviceUnits(new Size(1200, 720));
        MinimumSize = LogicalToDeviceUnits(new Size(640, 560));
        DoubleBuffered = true;

        var body = BuildBody();
        _header = BuildHeader();
        _footer = BuildFooter();

        Controls.Add(body);
        Controls.Add(_header);
        Controls.Add(_footer);
        body.BringToFront(); // corpul ocupa spatiul ramas intre header si footer

        Resize += (_, _) => ApplyMetrics();
        ApplyMetrics();
    }

    /// <summary>Marginile antetului / subsolului cresc si scad cu latimea ferestrei; sloganul dispare pe ferestre inguste.</summary>
    private void ApplyMetrics()
    {
        int inset = Math.Clamp(ClientSize.Width / 24, LogicalToDeviceUnits(20), LogicalToDeviceUnits(48));

        _header.Padding = new Padding(inset, 0, inset, LogicalToDeviceUnits(4));
        _footer.Padding = new Padding(inset, LogicalToDeviceUnits(2), inset, 0);
        _footer.Inset = inset;

        _tagline.Visible = ClientSize.Width >= LogicalToDeviceUnits(720);
    }

    // ===== Header: acelasi antet inchis ca in magazin =====
    private HeaderBar BuildHeader()
    {
        var header = new HeaderBar
        {
            Dock = DockStyle.Top,
            Height = LogicalToDeviceUnits(76),
        };

        _tagline = new Label
        {
            Text = "Magazin online de haine",
            Dock = DockStyle.Right,
            Width = LogicalToDeviceUnits(260),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Theme.Mix(Theme.Ink, Color.White, 0.62f),
            BackColor = Color.Transparent,
        };

        header.Controls.Add(_tagline);
        header.Controls.Add(new Wordmark { Dock = DockStyle.Left, Width = LogicalToDeviceUnits(240) });
        return header;
    }

    // ===== Corpul: panoul de brand + cardul de acces =====
    private Control BuildBody()
    {
        var hero = new HeroPanel();
        var access = new AccessCard();

        var login = access.AddOption("Autentificare",
            "Ai deja cont? Intra ca sa vezi comenzile si lista de dorinte.",
            AccessIcon.User, AccessStyle.Primary);
        var register = access.AddOption("Creeaza cont",
            "Primesti istoric de comenzi, comenzi active si wish list.",
            AccessIcon.UserPlus, AccessStyle.Secondary);
        var guest = access.AddOption("Intra fara cont",
            "Rasfoiesti si cumperi produse, fara wish list si istoric.",
            AccessIcon.Bag, AccessStyle.Ghost);

        login.Click += (_, _) => Authenticate(() => new LoginForm(_services));
        register.Click += (_, _) => Authenticate(() => new RegisterForm(_services));
        guest.Click += (_, _) => OpenShop();

        return new ResponsiveBody(hero, access) { Dock = DockStyle.Fill };
    }

    /// <summary>
    /// Pe ferestre late (&gt;= ~900 px) panoul de brand si cardul de acces stau una langa alta si umplu
    /// inaltimea; pe cele inguste se aseaza una sub alta (centrate, latime maxima ~680 px), iar corpul se
    /// deruleaza vertical daca nu incap.
    /// </summary>
    private sealed class ResponsiveBody : Panel
    {
        private readonly HeroPanel _hero;
        private readonly AccessCard _access;

        public ResponsiveBody(HeroPanel hero, AccessCard access)
        {
            _hero = hero;
            _access = access;

            DoubleBuffered = true;
            AutoScroll = true;
            BackColor = Theme.Concrete;

            Controls.Add(hero);
            Controls.Add(access);
        }

        /// <summary>Plaseaza cardul (fara umbra) exact in dreptunghiul dat; umbra ramane in marginea controlului.</summary>
        private static void Place(Control control, int x, int y, int width, int height)
        {
            var shadow = Surface.ShadowPadding(control);
            control.SetBounds(x - shadow.Left, y - shadow.Top, width + shadow.Horizontal, height + shadow.Vertical);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            int padX = Math.Clamp(ClientSize.Width / 24, LogicalToDeviceUnits(20), LogicalToDeviceUnits(48));
            int padY = Math.Clamp(ClientSize.Height / 16, LogicalToDeviceUnits(16), LogicalToDeviceUnits(28));
            int gap = LogicalToDeviceUnits(28);
            int width = ClientSize.Width - 2 * padX;
            int availableHeight = ClientSize.Height - 2 * padY;
            if (width <= 0 || availableHeight <= 0)
            {
                return;
            }

            int scrollY = AutoScrollPosition.Y;

            if (width >= LogicalToDeviceUnits(900))
            {
                int height = Math.Max(availableHeight, LogicalToDeviceUnits(520));
                int accessWidth = Math.Max(LogicalToDeviceUnits(440), (width - gap) / 2);
                int heroWidth = width - gap - accessWidth;

                Place(_hero, padX, padY + scrollY, heroWidth, height);
                Place(_access, padX + heroWidth + gap, padY + scrollY, accessWidth, height);
                AutoScrollMinSize = new Size(0, height + 2 * padY);
                return;
            }

            int maxWidth = Math.Min(width, LogicalToDeviceUnits(680));
            int x = padX + (width - maxWidth) / 2;
            int heroHeight = maxWidth >= LogicalToDeviceUnits(560)
                ? Math.Clamp((int)(maxWidth * 0.46), LogicalToDeviceUnits(280), LogicalToDeviceUnits(340))
                : LogicalToDeviceUnits(420);

            var shadow = Surface.ShadowPadding(_access);
            int accessHeight = _access.HeightFor(maxWidth + shadow.Horizontal) - shadow.Vertical;

            int total = heroHeight + gap + accessHeight;
            int extra = Math.Max(0, (availableHeight - total) / 2);

            Place(_hero, x, padY + extra + scrollY, maxWidth, heroHeight);
            Place(_access, x, padY + extra + heroHeight + gap + scrollY, maxWidth, accessHeight);
            AutoScrollMinSize = new Size(0, total + 2 * padY);
        }
    }

    // ===== Footer =====
    private LinePanel BuildFooter()
    {
        var footer = new LinePanel
        {
            Dock = DockStyle.Bottom,
            Height = LogicalToDeviceUnits(48),
            LineOnTop = true,
        };
        footer.Controls.Add(new Label
        {
            Text = "© 2026 MetroREX, un proiect de Timotei si George",
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
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
