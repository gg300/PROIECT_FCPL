using MetroREX.Controls;
using MetroREX.Models;
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
    private readonly RoundedButton _notificationsButton;
    private readonly RoundedButton _accountButton;
    private readonly ContextMenuStrip _accountMenu;
    private readonly Button _cartButton;
    private readonly Label _status;

    private Func<ViewBase>? _currentFactory;
    private ViewBase? _currentView;
    private bool _statusIsError;
    private string _errorOperation = string.Empty;
    private bool _modalOpen;

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
        _notificationsButton = Ui.NavButton(this, "Notificari", (_, _) => Open(() => new NotificationsView(this)));
        _accountMenu = BuildAccountMenu();
        _accountButton = Ui.NavButton(this, "Contul meu ▾", (_, _) => ShowAccountMenu());
        _cartButton = Ui.PrimaryButton(this, "Cos (0)", (_, _) => OpenCart());
        _cartButton.Margin = new Padding(LogicalToDeviceUnits(12), 0, 0, 0);

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
        _actions.Controls.Add(_notificationsButton);
        _actions.Controls.Add(_accountButton);
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

    public void SetCurrentFactory(Func<ViewBase> factory) => _currentFactory = factory;

    public void RefreshCartCount() =>
        _cartButton.Text = $"Cos ({Services.Cart.Current.Items.Sum(item => item.Quantity)})";

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

    private void Open<TView>(Func<TView> factory) where TView : ViewBase
    {
        if (_currentView is not TView)
        {
            Navigate(factory);
        }
    }

    private ContextMenuStrip BuildAccountMenu()
    {
        var menu = new ContextMenuStrip
        {
            Font = Theme.Body,
            ShowImageMargin = false,
            Padding = new Padding(LogicalToDeviceUnits(4)),
        };

        void Item(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem(text) { Padding = new Padding(LogicalToDeviceUnits(8), LogicalToDeviceUnits(6), LogicalToDeviceUnits(8), LogicalToDeviceUnits(6)) };
            item.Click += onClick;
            menu.Items.Add(item);
        }

        Item("Comenzi active", (_, _) => Open(() => new ActiveOrdersView(this)));
        Item("Istoricul comenzilor", (_, _) => Open(() => new OrderHistoryView(this)));
        Item("Retururi", (_, _) => Open(() => new ReturnsView(this)));
        Item("T-Rex Points", (_, _) => Open(() => new LoyaltyView(this)));
        Item("Wish list", (_, _) => Open(() => new WishListView(this)));
        Item("Cupoanele mele", (_, _) => Open(() => new CouponsView(this)));
        Item("Abonamente", (_, _) => Open(() => new SubscriptionsView(this)));
        Item("Setari", (_, _) => Open(() => new SettingsView(this)));
        menu.Items.Add(new ToolStripSeparator());
        Item("Logout", (_, _) => Logout());
        return menu;
    }

    private void ShowAccountMenu() =>
        _accountMenu.Show(_accountButton, new Point(0, _accountButton.Height));

    private void Logout()
    {
        Services.Auth.Logout();
        Close();
    }

    private void UpdateAccountButtons()
    {
        var user = Services.Session.CurrentUser;
        _notificationsButton.Visible = user is not null;
        _accountButton.Visible = user is not null;

        if (user is not null)
        {
            _accountButton.Text = $"{user.Username} ▾";
        }

        RefreshNotificationCount();
    }

    public void RefreshNotificationCount()
    {
        if (Services.Session.CurrentUser is not { } user)
        {
            return;
        }

        var unread = Services.Notifications.CountUnread(user);
        _notificationsButton.Text = unread > 0 ? $"Notificari ({unread})" : "Notificari";
    }

    private void UpdateActiveNav()
    {
        _categoriesButton.Selected = _currentView is CategoriesView or ProductListView or ProductView;
        _notificationsButton.Selected = _currentView is NotificationsView;
        _accountButton.Selected = _currentView is ActiveOrdersView or OrderHistoryView or WishListView
            or CouponsView or SubscriptionsView or SettingsView or ReturnsView or NewReturnView or LoyaltyView;
    }

    private void RefreshLiveView()
    {
        if (_currentView is ILiveView && _currentFactory is not null)
        {
            Display(_currentFactory);
        }
    }

    private void PromptRatingsIfNeeded()
    {
        if (_modalOpen || !Visible || !RatingDialog.HasPending(Services))
        {
            return;
        }

        _modalOpen = true;
        try
        {
            using var dialog = new RatingDialog(Services);
            dialog.ShowDialog(this);
        }
        finally
        {
            _modalOpen = false;
        }

        RefreshLiveView();
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

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Services.Runtime.ErrorOccurred += OnRuntimeError;
        Services.Runtime.ProductsSynchronized += OnProductsSynchronized;
        Services.Runtime.DayStarted += OnDayStarted;
        Services.Session.Changed += OnSessionChanged;
        Services.Notifications.NotificationAdded += OnNotificationAdded;

        GoHome();
        SetStatus("Sfat: Ctrl+Shift+T schimba data pentru testare (zi noua, livrari, cupoane).", isError: false);

        BeginInvoke(new Action(PromptRatingsIfNeeded));
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Services.Runtime.ErrorOccurred -= OnRuntimeError;
        Services.Runtime.ProductsSynchronized -= OnProductsSynchronized;
        Services.Runtime.DayStarted -= OnDayStarted;
        Services.Session.Changed -= OnSessionChanged;
        Services.Notifications.NotificationAdded -= OnNotificationAdded;
        _accountMenu.Dispose();

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

    private void OnDayStarted(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnDayStarted(sender, e)));
            return;
        }

        Sfx.Play(Sound.StationChime);
        RefreshLiveView();
        RefreshNotificationCount();
        SetStatus($"A inceput o zi noua ({Services.Clock.Today:dd-MM-yyyy}): preturile si cupoanele au fost actualizate.", isError: false);

        BeginInvoke(new Action(PromptRatingsIfNeeded));
    }

    private void OnNotificationAdded(object? sender, NotificationEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => OnNotificationAdded(sender, e)));
            return;
        }

        if (!ReferenceEquals(e.User, Services.Session.CurrentUser))
        {
            return;
        }

        RefreshNotificationCount();
        if (e.Notification.Type != NotificationType.Info)
        {
            SetStatus(e.Notification.Message, isError: false);
        }
    }

    private void SetStatus(string text, bool isError)
    {
        _status.Text = text;
        _status.ForeColor = isError ? Ui.Danger : Theme.Muted;
        _statusIsError = isError;
    }

    // Metoda speciala pentru schimbarea datei (testare): Ctrl+Shift+T, Ctrl+Shift+D (+1 zi), Ctrl+Shift+R (ceas real)

    private void OpenDebugClock()
    {
        if (_modalOpen)
        {
            return;
        }

        _modalOpen = true;
        try
        {
            using var dialog = new DebugClockDialog(Services.Clock);
            dialog.ShowDialog(this);
        }
        finally
        {
            _modalOpen = false;
        }

        ReportClock();
        PromptRatingsIfNeeded();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.Shift | Keys.T:
                OpenDebugClock();
                return true;

            case Keys.Control | Keys.Shift | Keys.D:
                Services.Clock.AdvanceDays(1);
                ReportClock();
                return true;

            case Keys.Control | Keys.M:
                Sfx.Muted = !Sfx.Muted;
                Sfx.Play(Sound.Tick);
                SetStatus(Sfx.Muted ? "Sunet oprit (Ctrl+M pentru a-l porni)." : "Sunet pornit (Ctrl+M pentru a-l opri).", isError: false);
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
