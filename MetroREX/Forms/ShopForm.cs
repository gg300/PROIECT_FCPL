using MetroREX.Controls;
using MetroREX.Services;

namespace MetroREX.Forms;

internal sealed class ShopForm : Form
{
    private const string SyncOperation = "Sincronizare Excel"; // acelasi text ca in ShopRuntime

    private readonly Stack<Func<ViewBase>> _history = new();
    private readonly Panel _host;
    private readonly FlowLayoutPanel _actions;
    private readonly RoundedButton _backButton;
    private readonly RoundedButton _categoriesButton;
    private readonly RoundedButton _subscriptionsButton;
    private readonly Button _cartButton;
    private readonly Label _status;

    private Func<ViewBase>? _currentFactory;
    private ViewBase? _currentView;
    private bool _statusIsError;
    private string _errorOperation = string.Empty;

    public ShopForm(ShopServices services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));

        Text = "MetroREX";
        BackColor = Theme.Concrete;
        ForeColor = Theme.Ink;
        Font = Theme.Body;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = LogicalToDeviceUnits(new Size(1200, 720));
        MinimumSize = LogicalToDeviceUnits(new Size(1000, 640));
        DoubleBuffered = true;

        _backButton = Ui.NavButton(this, "Inapoi", (_, _) => Back(), arrow: true);
        _backButton.Visible = false;
        _categoriesButton = Ui.NavButton(this, "Categorii", (_, _) => GoHome());
        _subscriptionsButton = Ui.NavButton(this, "Abonamente", (_, _) => OpenSubscriptions());
        _cartButton = Ui.PrimaryButton(this, "Cos (0)", (_, _) => OpenCart());
        _cartButton.Margin = new Padding(LogicalToDeviceUnits(12), 0, 0, 0);

        // TODO (pasul Login): aici apar si Comenzi active, Istoric, Wish list, Cupoane, Setari, Logout,
        // afisate doar cand Services.Session.IsLoggedIn (vezi evenimentul Session.Changed).
        _actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.Transparent,
            Padding = new Padding(0, LogicalToDeviceUnits(16), 0, 0),
        };
        _actions.Controls.Add(_backButton);
        _actions.Controls.Add(_categoriesButton);
        _actions.Controls.Add(_subscriptionsButton);
        _actions.Controls.Add(_cartButton);

        var wordmark = new Wordmark { Dock = DockStyle.Left, Width = LogicalToDeviceUnits(240) };
        wordmark.Click += (_, _) => GoHome();

        var header = new HeaderBar
        {
            Dock = DockStyle.Top,
            Height = LogicalToDeviceUnits(76),
            Padding = new Padding(LogicalToDeviceUnits(48), 0, LogicalToDeviceUnits(48), LogicalToDeviceUnits(4)),
        };
        header.Controls.Add(_actions);
        header.Controls.Add(wordmark);

        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = LogicalToDeviceUnits(30),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(LogicalToDeviceUnits(48), 0, LogicalToDeviceUnits(48), 0),
            ForeColor = Theme.Muted,
        };

        _host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Concrete };

        Controls.Add(_host);
        Controls.Add(header);
        Controls.Add(_status);
        _host.BringToFront(); // zona ecranelor ocupa spatiul ramas intre antet si bara de stare

        UpdateAccountButtons();
    }

    public ShopServices Services { get; }

    // ===== Navigare =====

    /// <summary>Deschide un ecran nou; cel curent ramane in istoric pentru „Inapoi”.</summary>
    public void Navigate(Func<ViewBase> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (_currentFactory is not null)
        {
            _history.Push(_currentFactory);
        }

        Display(factory);
    }

    public void Back()
    {
        if (_history.Count > 0)
        {
            Display(_history.Pop());
        }
    }

    public void GoHome()
    {
        _history.Clear();
        _currentFactory = null;
        Navigate(() => new CategoriesView(this));
    }

    /// <summary>Schimba „reteta” ecranului curent fara sa-l reconstruiasca (ex.: ordinea de sortare aleasa).</summary>
    public void SetCurrentFactory(Func<ViewBase> factory) => _currentFactory = factory;

    public void RefreshCartCount() =>
        _cartButton.Text = $"Cos ({Services.Cart.Current.Items.Sum(item => item.Quantity)})";

    /// <summary>Blocheaza navigarea cat timp se trimite o comanda.</summary>
    public void SetBusy(bool busy)
    {
        _actions.Enabled = !busy;
        _host.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void OpenCart()
    {
        if (_currentView is not CartView)
        {
            Navigate(() => new CartView(this));
        }
    }

    private void OpenSubscriptions()
    {
        if (_currentView is not SubscriptionsView)
        {
            Navigate(() => new SubscriptionsView(this));
        }
    }

    /// <summary>Butoanele rezervate utilizatorilor autentificati.</summary>
    private void UpdateAccountButtons() => _subscriptionsButton.Visible = Services.Session.IsLoggedIn;

    /// <summary>Linia portocalie de sub sectiunea curenta din antet.</summary>
    private void UpdateActiveNav()
    {
        _categoriesButton.Selected = _currentView is CategoriesView or ProductListView;
        _subscriptionsButton.Selected = _currentView is SubscriptionsView;
    }

    private void Display(Func<ViewBase> factory)
    {
        var view = factory();
        var previous = _currentView;

        _currentFactory = factory;
        _currentView = view;

        _host.SuspendLayout();
        if (previous is not null)
        {
            _host.Controls.Remove(previous);
            BeginInvoke(new Action(previous.Dispose)); // nu distrugem ecranul din mijlocul unui eveniment al lui
        }

        _host.Controls.Add(view);
        _host.ResumeLayout(true);

        _backButton.Visible = _history.Count > 0;
        UpdateActiveNav();
        RefreshCartCount();

        if (!_statusIsError)
        {
            _status.Text = string.Empty;
        }
    }

    // ===== Procese de fundal =====

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Services.Runtime.ErrorOccurred += OnRuntimeError;
        Services.Runtime.ProductsSynchronized += OnProductsSynchronized;
        Services.Runtime.DayStarted += OnDayStarted;
        Services.Session.Changed += OnSessionChanged;

        GoHome();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Services.Runtime.ErrorOccurred -= OnRuntimeError;
        Services.Runtime.ProductsSynchronized -= OnProductsSynchronized;
        Services.Runtime.DayStarted -= OnDayStarted;
        Services.Session.Changed -= OnSessionChanged;

        base.OnFormClosed(e);
    }

    private void OnRuntimeError(object? sender, RuntimeErrorEventArgs e)
    {
        _errorOperation = e.Operation;
        SetStatus($"{e.Operation}: {e.Exception.Message}", isError: true);
    }

    private void OnProductsSynchronized(object? sender, EventArgs e)
    {
        if (_statusIsError && _errorOperation == SyncOperation)
        {
            SetStatus(string.Empty, isError: false);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e) => UpdateAccountButtons();

    private void OnDayStarted(object? sender, EventArgs e) =>
        SetStatus("A inceput o zi noua: preturile si cupoanele au fost actualizate.", isError: false);

    private void SetStatus(string text, bool isError)
    {
        _status.Text = text;
        _status.ForeColor = isError ? Ui.Danger : Theme.Muted;
        _statusIsError = isError;
    }

    // ===== Schimbarea datei pentru testare (cerinta „metoda speciala pentru data/ora”) =====
    // Ctrl+Shift+D = +1 zi, Ctrl+Shift+R = inapoi la ceasul real.

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Shift | Keys.D:
                Services.Clock.AdvanceDays(1);
                ReportClock();
                return true;

            case Keys.Control | Keys.Shift | Keys.R:
                Services.Clock.Reset();
                ReportClock();
                return true;

            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void ReportClock() => SetStatus(
        Services.Clock.IsOverridden
            ? $"Mod test: data simulata {Services.Clock.Now:dd-MM-yyyy HH:mm}"
            : "Ceasul a fost resetat la ora reala.",
        isError: false);
}
