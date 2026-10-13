using MetroREX.Models;

namespace MetroREX.Forms;

/// <summary>
/// Notificarile utilizatorului: schimbari de pret si produse revenite pe stoc (din wish list), livrari.
/// La deschidere toate devin citite.
/// </summary>
internal sealed class NotificationsView : ViewBase, ILiveView
{
    private readonly DataGridView _grid;
    private readonly Label _summary;

    public NotificationsView(ShopForm shell) : base(shell)
    {
        var column = Ui.Column(this);
        Ui.AddRow(column, Ui.Title(this, "Notificari"));

        _summary = Ui.Muted(this, string.Empty);
        Ui.AddRow(column, _summary);

        _grid = Ui.Grid(this,
            ("Data", 16f, false),
            ("Tip", 16f, false),
            ("Mesaj", 68f, false));
        _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        Ui.AddRow(column, _grid, fill: true);

        Controls.Add(column);
        Reload();
    }

    private void Reload()
    {
        if (Services.Session.CurrentUser is not { } user)
        {
            return;
        }

        var notifications = Services.Notifications.GetAll(user);
        var unread = Services.Notifications.CountUnread(user);

        _grid.Rows.Clear();
        foreach (var notification in notifications)
        {
            var index = _grid.Rows.Add(
                notification.Date.ToString("dd-MM-yyyy HH:mm"),
                Describe(notification.Type),
                notification.Message);

            if (!notification.IsRead)
            {
                _grid.Rows[index].DefaultCellStyle.Font = Ui.BoldFont;
            }
        }

        _summary.Text = notifications.Count == 0
            ? "Nu ai notificari. Adauga produse in wish list ca sa afli cand li se schimba pretul."
            : unread > 0 ? $"{unread} necitite (afisate ingrosat)." : "Toate notificarile sunt citite.";

        Services.Notifications.MarkAllRead(user);
        Shell.RefreshNotificationCount();
    }

    private static string Describe(NotificationType type) => type switch
    {
        NotificationType.PriceChanged => "Pret schimbat",
        NotificationType.BackInStock => "Din nou pe stoc",
        _ => "Informatie",
    };
}
