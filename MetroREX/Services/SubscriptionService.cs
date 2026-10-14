using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record SellerOffer(string Name, int ProductCount, bool IsSubscribed);

internal sealed class SubscriptionService
{
    public const int DiscountPercent = 10;
    public const int MaxSubscriptions = 3;

    private readonly ShopRepository _repository;
    private readonly Session _session;

    public SubscriptionService(ShopRepository repository, Session session)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public IReadOnlyList<SellerOffer> GetSellers() => _repository.Products
        .Where(product => !string.IsNullOrWhiteSpace(product.Seller))
        .GroupBy(product => product.Seller, StringComparer.OrdinalIgnoreCase)
        .Select(group => new SellerOffer(group.Key, group.Count(), IsSubscribed(group.Key)))
        .OrderBy(offer => offer.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public bool IsSubscribed(string seller) =>
        _session.CurrentUser?.Subscriptions.Contains(seller, StringComparer.OrdinalIgnoreCase) ?? false;

    public decimal DiscountFor(Product product, decimal lineTotal)
    {
        ArgumentNullException.ThrowIfNull(product);

        return IsSubscribed(product.Seller)
            ? Math.Round(lineTotal * DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    public Result Subscribe(string seller)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        var offers = GetSellers();
        var offer = offers.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, seller, StringComparison.OrdinalIgnoreCase));

        if (offer is null)
        {
            return Result.Failure("Vanzatorul nu a fost gasit.");
        }

        if (offer.IsSubscribed)
        {
            return Result.Failure($"Esti deja abonat la „{offer.Name}”.");
        }

        // Se numara doar vanzatorii care mai exista in catalog: un vanzator disparut din Excel nu blocheaza limita.
        if (offers.Count(candidate => candidate.IsSubscribed) >= MaxSubscriptions)
        {
            return Result.Failure(
                $"Poti avea cel mult {MaxSubscriptions} abonamente. Renunta la unul ca sa adaugi altul.");
        }

        user.Subscriptions.Add(offer.Name);
        _repository.SaveUsers();
        return Result.Success();
    }

    public Result Unsubscribe(string seller)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        var removed = user.Subscriptions.RemoveAll(name =>
            string.Equals(name, seller, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
        {
            return Result.Failure("Nu esti abonat la acest vanzator.");
        }

        _repository.SaveUsers();
        return Result.Success();
    }
}
