using System.Globalization;
using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class NotificationEventArgs : EventArgs
{
    public NotificationEventArgs(User user, Notification notification)
    {
        User = user;
        Notification = notification;
    }

    public User User { get; }

    public Notification Notification { get; }
}

internal sealed class NotificationService
{
    private const int MaxStoredPerUser = 200;

    private readonly ShopRepository _repository;
    private readonly IClock _clock;

    public NotificationService(ShopRepository repository, IClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public event EventHandler<NotificationEventArgs>? NotificationAdded;

    public Notification Notify(User user, NotificationType type, string message)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var notification = new Notification
        {
            Type = type,
            Message = message,
            Date = _clock.Now,
            IsRead = false,
        };

        user.Notifications.Add(notification);

        var overflow = user.Notifications.Count - MaxStoredPerUser;
        if (overflow > 0)
        {
            user.Notifications.RemoveRange(0, overflow);
        }

        _repository.QueueUsersSave();
        NotificationAdded?.Invoke(this, new NotificationEventArgs(user, notification));
        return notification;
    }

    public void NotifyPriceChange(Product product, decimal oldPrice, decimal newPrice)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (oldPrice == newPrice)
        {
            return;
        }

        var percent = oldPrice <= 0
            ? 0
            : Math.Round(Math.Abs(newPrice - oldPrice) / oldPrice * 100m, MidpointRounding.AwayFromZero);
        var direction = newPrice > oldPrice ? "a crescut" : "a scăzut";

        var message = string.Create(
            CultureInfo.InvariantCulture,
            $"Produsul „{product.Name}”: prețul {direction} cu {percent}%, de la {Money.Format(oldPrice)} la {Money.Format(newPrice)}.");

        NotifyWishListOwners(product.Id, NotificationType.PriceChanged, message);
    }

    public void NotifyBackInStock(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        NotifyWishListOwners(
            product.Id,
            NotificationType.BackInStock,
            $"Produsul „{product.Name}” este din nou disponibil.");
    }

    public IReadOnlyList<Notification> GetAll(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Notifications.OrderByDescending(notification => notification.Date).ToList();
    }

    public int CountUnread(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return user.Notifications.Count(notification => !notification.IsRead);
    }

    public void MarkAllRead(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var changed = false;
        foreach (var notification in user.Notifications.Where(notification => !notification.IsRead))
        {
            notification.IsRead = true;
            changed = true;
        }

        if (changed)
        {
            _repository.QueueUsersSave();
        }
    }

    private void NotifyWishListOwners(string productId, NotificationType type, string message)
    {
        var owners = _repository.Users
            .Where(user => user.WishList.Contains(productId, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var owner in owners)
        {
            Notify(owner, type, message);
        }
    }
}
