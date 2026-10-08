using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class PricingService
{
    private const decimal MaxVariation = 0.20m;
    private const decimal MinimumPrice = 0.01m;

    private readonly ShopRepository _repository;
    private readonly NotificationService _notifications;
    private readonly Random _random;

    public PricingService(ShopRepository repository, NotificationService notifications, Random random)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public int ApplyDailyVariation()
    {
        var changed = 0;

        foreach (var product in _repository.Products)
        {
            var previous = product.CurrentPrice;
            var updated = NextPrice(product.BasePrice);

            if (updated == previous)
            {
                continue;
            }

            product.CurrentPrice = updated;
            _notifications.NotifyPriceChange(product, previous, updated);
            changed++;
        }

        return changed;
    }

    private decimal NextPrice(decimal basePrice)
    {
        var factor = (decimal)(_random.NextDouble() * 2d - 1d) * MaxVariation;
        var price = Math.Round(basePrice * (1m + factor), 2, MidpointRounding.AwayFromZero);

        var lowest = Math.Max(MinimumPrice, Math.Round(basePrice * (1m - MaxVariation), 2, MidpointRounding.AwayFromZero));
        var highest = Math.Max(lowest, Math.Round(basePrice * (1m + MaxVariation), 2, MidpointRounding.AwayFromZero));

        return Math.Clamp(price, lowest, highest);
    }
}
