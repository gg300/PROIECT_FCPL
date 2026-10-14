using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record ReturnLineRequest(string ProductId, int Quantity, string Reason);

internal sealed record ReturnableItem(OrderItem Item, int AlreadyReturned)
{
    public int Available => Math.Max(0, Item.Quantity - AlreadyReturned);
}

internal sealed class ReturnService
{
    public const int ReturnWindowDays = 14;
    public const decimal PointsRefundBonus = 0.10m;

    public static readonly IReadOnlyList<string> Reasons = new[]
    {
        "Marime nepotrivita",
        "Produs defect",
        "Nu corespunde descrierii",
        "Am primit alt produs",
        "M-am razgandit",
    };

    private readonly ShopRepository _repository;
    private readonly Session _session;
    private readonly IClock _clock;
    private readonly NotificationService _notifications;
    private readonly LoyaltyService _loyalty;

    public ReturnService(
        ShopRepository repository,
        Session session,
        IClock clock,
        NotificationService notifications,
        LoyaltyService loyalty,
        ShopRuntime runtime)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _loyalty = loyalty ?? throw new ArgumentNullException(nameof(loyalty));
        ArgumentNullException.ThrowIfNull(runtime);

        runtime.DayStarted += (_, _) => ProcessDaySafely();
    }

    public string? LastError { get; private set; }

    public IReadOnlyList<ReturnRequest> GetReturns() => _session.CurrentUser is { } user
        ? user.Returns.OrderByDescending(request => request.RequestDate).ToList()
        : Array.Empty<ReturnRequest>();

    public IReadOnlyList<Order> GetEligibleOrders()
    {
        if (_session.CurrentUser is not { } user)
        {
            return Array.Empty<Order>();
        }

        return user.Orders
            .Where(order => order.Status == OrderStatus.Delivered && DaysLeft(order) >= 0)
            .Where(order => GetReturnableItems(user, order).Any(item => item.Available > 0))
            .OrderByDescending(order => order.DeliveryDate)
            .ToList();
    }

    public int DaysLeft(Order order) =>
        (order.DeliveryDate.Date.AddDays(ReturnWindowDays) - _clock.Today).Days;

    public IReadOnlyList<ReturnableItem> GetReturnableItems(Order order) =>
        _session.CurrentUser is { } user ? GetReturnableItems(user, order) : Array.Empty<ReturnableItem>();

    public int ReturnedQuantity(Order order, string productId) =>
        _session.CurrentUser is { } user ? ReturnedQuantity(user, order.Id, productId) : 0;

    public static decimal RefundFor(Order order, IEnumerable<(decimal UnitPrice, int Quantity)> lines)
    {
        ArgumentNullException.ThrowIfNull(order);

        var value = lines.Sum(line => line.UnitPrice * line.Quantity);
        var ratio = order.Subtotal > 0 ? order.TotalCost / order.Subtotal : 0m;
        return Math.Round(value * ratio, 2, MidpointRounding.AwayFromZero);
    }

    public static int PointsForRefund(decimal amount) =>
        amount <= 0 ? 0 : (int)Math.Floor(amount / LoyaltyService.PointValue * (1m + PointsRefundBonus));

    public static string StatusText(ReturnStatus status) => status switch
    {
        ReturnStatus.Requested => "Cerere trimisa",
        ReturnStatus.Approved => "Aprobat, se ridica coletul",
        ReturnStatus.Completed => "Finalizat",
        _ => status.ToString(),
    };

    public Result<ReturnRequest> Create(string orderId, IReadOnlyList<ReturnLineRequest> lines, RefundMethod method)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure<ReturnRequest>(ErrorMessages.LoginRequired);
        }

        var order = user.Orders.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, orderId, StringComparison.OrdinalIgnoreCase));

        if (order is null)
        {
            return Result.Failure<ReturnRequest>("Comanda nu a fost gasita.");
        }

        if (order.Status != OrderStatus.Delivered)
        {
            return Result.Failure<ReturnRequest>("Se pot returna doar produse din comenzi livrate.");
        }

        if (DaysLeft(order) < 0)
        {
            return Result.Failure<ReturnRequest>($"Termenul de retur de {ReturnWindowDays} zile a expirat.");
        }

        var requested = (lines ?? Array.Empty<ReturnLineRequest>()).Where(line => line.Quantity > 0).ToList();
        if (requested.Count == 0)
        {
            return Result.Failure<ReturnRequest>("Alege cel putin un produs si cantitatea de returnat.");
        }

        var returnLines = new List<ReturnLine>();
        foreach (var line in requested)
        {
            var item = order.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.ProductId, line.ProductId, StringComparison.OrdinalIgnoreCase));

            if (item is null)
            {
                return Result.Failure<ReturnRequest>(ErrorMessages.ProductNotFound);
            }

            if (returnLines.Any(existing => string.Equals(existing.ProductId, item.ProductId, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<ReturnRequest>($"„{item.ProductName}” apare de doua ori in cerere.");
            }

            var available = item.Quantity - ReturnedQuantity(user, order.Id, item.ProductId);
            if (line.Quantity > available)
            {
                return Result.Failure<ReturnRequest>(available <= 0
                    ? $"„{item.ProductName}” a fost deja returnat."
                    : $"Din „{item.ProductName}” se mai pot returna cel mult {available} bucati.");
            }

            if (string.IsNullOrWhiteSpace(line.Reason))
            {
                return Result.Failure<ReturnRequest>($"Alege motivul returului pentru „{item.ProductName}”.");
            }

            returnLines.Add(new ReturnLine
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitPrice = item.UnitPrice,
                Quantity = line.Quantity,
                Reason = line.Reason.Trim(),
            });
        }

        var amount = RefundFor(order, returnLines.Select(line => (line.UnitPrice, line.Quantity)));
        var request = new ReturnRequest
        {
            OrderId = order.Id,
            OrderShortId = order.ShortId,
            RequestDate = _clock.Now,
            Status = ReturnStatus.Requested,
            RefundMethod = method,
            Lines = returnLines,
            RefundAmount = amount,
            RefundPoints = method == RefundMethod.LoyaltyPoints ? PointsForRefund(amount) : 0,
        };

        user.Returns.Add(request);
        _notifications.Notify(user, NotificationType.Info,
            $"Cererea de retur {request.ShortId} pentru comanda {order.ShortId} a fost inregistrata ({amount:N2} lei).");
        _repository.SaveUsers();
        return Result.Success(request);
    }

    public int ProcessDay()
    {
        var today = _clock.Today;
        var changedUsers = false;
        var changedProducts = false;
        var advanced = 0;

        foreach (var user in _repository.Users)
        {
            foreach (var request in user.Returns.Where(request => request.Status != ReturnStatus.Completed))
            {
                if (request.Status == ReturnStatus.Requested && today >= request.RequestDate.Date.AddDays(1))
                {
                    request.Status = ReturnStatus.Approved;
                    request.ApprovedDate = request.RequestDate.Date.AddDays(1);
                    _notifications.Notify(user, NotificationType.Info,
                        $"Returul {request.ShortId} a fost aprobat. Curierul ridica coletul.");
                    changedUsers = true;
                    advanced++;
                }

                if (request.Status == ReturnStatus.Approved
                    && request.ApprovedDate is { } approved
                    && today >= approved.Date.AddDays(1))
                {
                    changedProducts |= Complete(user, request);
                    changedUsers = true;
                    advanced++;
                }
            }
        }

        if (changedProducts)
        {
            _repository.SaveProducts();
        }

        if (changedUsers)
        {
            _repository.SaveUsers();
        }

        return advanced;
    }

    private bool Complete(User user, ReturnRequest request)
    {
        var restocked = false;
        foreach (var line in request.Lines)
        {
            var product = _repository.Products.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, line.ProductId, StringComparison.OrdinalIgnoreCase));

            if (product is not null)
            {
                product.Quantity += line.Quantity;
                restocked = true;
            }
        }

        var order = user.Orders.FirstOrDefault(candidate => candidate.Id == request.OrderId);
        if (order is not null)
        {
            _loyalty.ReverseForReturn(user, order, request.RefundAmount, request.ShortId);
        }

        string refundText;
        if (request.RefundMethod == RefundMethod.LoyaltyPoints)
        {
            _loyalty.Credit(user, request.RefundPoints, $"Rambursare retur {request.ShortId}", request.OrderId,
                isOrderEarning: false, countsTowardTier: false);
            refundText = $"ai primit {request.RefundPoints} T-Rex Points (valoare {LoyaltyService.ValueOf(request.RefundPoints):N2} lei)";
        }
        else
        {
            var target = order?.PaymentMethod == PaymentMethod.Card ? "pe card" : "in numerar, prin curier";
            refundText = $"{request.RefundAmount:N2} lei au fost rambursati {target}";
        }

        request.Status = ReturnStatus.Completed;
        request.CompletedDate = request.ApprovedDate!.Value.Date.AddDays(1);

        _notifications.Notify(user, NotificationType.Info, $"Returul {request.ShortId} a fost finalizat: {refundText}.");
        return restocked;
    }

    private void ProcessDaySafely()
    {
        try
        {
            ProcessDay();
            LastError = null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                                              or InvalidOperationException or InvalidDataException)
        {
            LastError = exception.Message;
        }
    }

    private static IReadOnlyList<ReturnableItem> GetReturnableItems(User user, Order order) =>
        order.Items.Select(item => new ReturnableItem(item, ReturnedQuantity(user, order.Id, item.ProductId))).ToList();

    private static int ReturnedQuantity(User user, string orderId, string productId) => user.Returns
        .Where(request => request.OrderId == orderId)
        .SelectMany(request => request.Lines)
        .Where(line => string.Equals(line.ProductId, productId, StringComparison.OrdinalIgnoreCase))
        .Sum(line => line.Quantity);
}
