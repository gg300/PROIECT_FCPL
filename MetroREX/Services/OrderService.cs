using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class OrderDeliveredEventArgs : EventArgs
{
    public OrderDeliveredEventArgs(User user, Order order)
    {
        User = user;
        Order = order;
    }

    public User User { get; }

    public Order Order { get; }
}

internal sealed record PendingRating(Order Order, OrderItem Item);

internal sealed class OrderService
{
    private readonly ShopRepository _repository;
    private readonly Session _session;
    private readonly IClock _clock;
    private readonly NotificationService _notifications;

    public OrderService(ShopRepository repository, Session session, IClock clock, NotificationService notifications)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    public event EventHandler<OrderDeliveredEventArgs>? OrderDelivered;

    public IReadOnlyList<Order> GetActiveOrders() => _session.CurrentUser is { } user
        ? user.Orders
            .Where(order => order.Status == OrderStatus.Active)
            .OrderBy(order => order.DeliveryDate)
            .ToList()
        : Array.Empty<Order>();

    public IReadOnlyList<Order> GetHistory() => _session.CurrentUser is { } user
        ? user.Orders
            .Where(order => order.Status == OrderStatus.Delivered)
            .OrderByDescending(order => order.DeliveryDate)
            .ThenByDescending(order => order.OrderDate)
            .ToList()
        : Array.Empty<Order>();

    public IReadOnlyList<PendingRating> GetPendingRatings() => _session.CurrentUser is { } user
        ? user.Orders
            .Where(order => order.Status == OrderStatus.Delivered)
            .OrderBy(order => order.DeliveryDate)
            .SelectMany(order => order.Items
                .Where(item => item.Rating is null)
                .Select(item => new PendingRating(order, item)))
            .ToList()
        : Array.Empty<PendingRating>();

    public Result Rate(string orderId, string productId, int stars)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        if (stars is < 1 or > 5)
        {
            return Result.Failure("Nota trebuie sa fie intre 1 si 5.");
        }

        var order = user.Orders.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, orderId, StringComparison.OrdinalIgnoreCase));

        if (order is null)
        {
            return Result.Failure("Comanda nu a fost gasita.");
        }

        if (order.Status != OrderStatus.Delivered)
        {
            return Result.Failure("Poti evalua produsele doar dupa livrare.");
        }

        var item = order.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.ProductId, productId, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            return Result.Failure(ErrorMessages.ProductNotFound);
        }

        if (item.Rating is not null)
        {
            return Result.Failure("Acest produs a fost deja evaluat.");
        }

        item.Rating = stars;

        var product = _repository.Products.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, item.ProductId, StringComparison.OrdinalIgnoreCase));

        if (product is not null)
        {
            product.AddRating(stars);
            _repository.SaveProducts();
        }

        _repository.SaveUsers();
        return Result.Success();
    }

    public int ProcessDeliveries()
    {
        var now = _clock.Now;
        var delivered = new List<(User User, Order Order)>();

        foreach (var user in _repository.Users)
        {
            foreach (var order in user.Orders.Where(order => order.Status == OrderStatus.Active && order.IsDelivered(now)))
            {
                order.Status = OrderStatus.Delivered;
                delivered.Add((user, order));
            }
        }

        if (delivered.Count == 0)
        {
            return 0;
        }

        foreach (var (user, order) in delivered)
        {
            _notifications.Notify(
                user,
                NotificationType.Info,
                $"Comanda {order.ShortId} a fost livrata. Te rugam sa evaluezi produsele primite.");
        }

        _repository.SaveUsers();

        foreach (var (user, order) in delivered)
        {
            OrderDelivered?.Invoke(this, new OrderDeliveredEventArgs(user, order));
        }

        return delivered.Count;
    }
}
