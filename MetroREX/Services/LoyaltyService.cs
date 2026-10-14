using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record TierInfo(LoyaltyTier Tier, int FromLifetimePoints, decimal Multiplier);

internal sealed class LoyaltyService
{
    public const decimal LeiPerEarnedPoint = 10m;
    public const decimal PointValue = 0.10m;
    public const decimal MaxRedeemShare = 0.50m;
    public const int WelcomeBonus = 50;

    public static readonly IReadOnlyList<TierInfo> Tiers = new[]
    {
        new TierInfo(LoyaltyTier.Bronz, 0, 1.00m),
        new TierInfo(LoyaltyTier.Argint, 200, 1.25m),
        new TierInfo(LoyaltyTier.Aur, 500, 1.50m),
    };

    private readonly ShopRepository _repository;
    private readonly Session _session;
    private readonly IClock _clock;
    private readonly NotificationService _notifications;

    public LoyaltyService(
        ShopRepository repository,
        Session session,
        IClock clock,
        NotificationService notifications,
        OrderService orders,
        AuthService auth)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        ArgumentNullException.ThrowIfNull(orders);

        orders.OrderDelivered += (_, e) => AwardForDeliveredOrder(e.User, e.Order);

        ArgumentNullException.ThrowIfNull(auth);
        auth.UserRegistered += (_, user) => Credit(
            user, WelcomeBonus, "Bonus de bun venit",
            orderId: null, isOrderEarning: false, countsTowardTier: false);
    }

    public static TierInfo TierFor(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Tiers.Last(tier => user.LifetimePoints >= tier.FromLifetimePoints);
    }

    public static (TierInfo Next, int Missing)? NextTier(User user)
    {
        var next = Tiers.FirstOrDefault(tier => tier.FromLifetimePoints > user.LifetimePoints);
        return next is null ? null : (next, next.FromLifetimePoints - user.LifetimePoints);
    }

    public static string TierName(LoyaltyTier tier) => tier.ToString();

    public static decimal ValueOf(int points) => points * PointValue;

    public int MaxRedeemable(decimal amountDue)
    {
        if (_session.CurrentUser is not { } user || amountDue <= 0)
        {
            return 0;
        }

        var byAmount = (int)Math.Floor(amountDue * MaxRedeemShare / PointValue);
        return Math.Max(0, Math.Min(user.LoyaltyPoints, byAmount));
    }

    public void Spend(User user, int points, Order order)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(order);

        if (points <= 0)
        {
            return;
        }

        if (points > user.LoyaltyPoints)
        {
            throw new InvalidOperationException("Sold insuficient de puncte.");
        }

        user.LoyaltyPoints -= points;
        AddEntry(user, -points, $"Folosite la comanda {order.ShortId} (-{ValueOf(points):N2} lei)", order.Id, isOrderEarning: false);
    }

    public static int PointsFor(User user, decimal paidAmount)
    {
        if (paidAmount <= 0)
        {
            return 0;
        }

        var basePoints = Math.Floor(paidAmount / LeiPerEarnedPoint);
        return (int)Math.Floor(basePoints * TierFor(user).Multiplier);
    }

    private void AwardForDeliveredOrder(User user, Order order)
    {
        var points = PointsFor(user, order.TotalCost);
        if (points <= 0)
        {
            return;
        }

        var tierBefore = TierFor(user).Tier;
        Credit(user, points, $"Comanda livrata {order.ShortId}", order.Id, isOrderEarning: true);

        var message = $"Ai primit {points} T-Rex Points pentru comanda {order.ShortId}. Sold: {user.LoyaltyPoints} puncte.";
        var tierAfter = TierFor(user).Tier;
        if (tierAfter != tierBefore)
        {
            message += $" Felicitari, ai ajuns la nivelul {TierName(tierAfter)}!";
        }

        _notifications.Notify(user, NotificationType.Info, message);
        _repository.SaveUsers();
    }

    public void Credit(User user, int points, string reason, string? orderId, bool isOrderEarning, bool countsTowardTier = true)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (points <= 0)
        {
            return;
        }

        user.LoyaltyPoints += points;
        if (countsTowardTier)
        {
            user.LifetimePoints += points;
        }

        AddEntry(user, points, reason, orderId, isOrderEarning);
    }

    public int ReverseForReturn(User user, Order order, decimal refundAmount, string returnId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(order);

        var earned = user.LoyaltyHistory
            .Where(entry => entry.IsOrderEarning && entry.OrderId == order.Id)
            .Sum(entry => entry.Points);

        if (earned <= 0 || order.TotalCost <= 0 || refundAmount <= 0)
        {
            return 0;
        }

        var share = Math.Min(1m, refundAmount / order.TotalCost);
        var toReverse = (int)Math.Round(earned * share, MidpointRounding.AwayFromZero);
        toReverse = Math.Min(toReverse, user.LoyaltyPoints);

        if (toReverse <= 0)
        {
            return 0;
        }

        user.LoyaltyPoints -= toReverse;
        user.LifetimePoints = Math.Max(0, user.LifetimePoints - toReverse);
        AddEntry(user, -toReverse, $"Anulate pentru returul {returnId}", order.Id, isOrderEarning: false);
        return toReverse;
    }

    public IReadOnlyList<LoyaltyEntry> GetHistory() => _session.CurrentUser is { } user
        ? user.LoyaltyHistory.OrderByDescending(entry => entry.Date).ToList()
        : Array.Empty<LoyaltyEntry>();

    private void AddEntry(User user, int points, string reason, string? orderId, bool isOrderEarning) =>
        user.LoyaltyHistory.Add(new LoyaltyEntry
        {
            Date = _clock.Now,
            Points = points,
            Reason = reason,
            OrderId = orderId,
            IsOrderEarning = isOrderEarning,
        });
}
