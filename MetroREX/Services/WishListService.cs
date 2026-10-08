using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class WishListService
{
    private readonly ShopRepository _repository;
    private readonly Session _session;

    public WishListService(ShopRepository repository, Session session)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public bool Contains(string productId) =>
        _session.CurrentUser?.WishList.Contains(productId, StringComparer.OrdinalIgnoreCase) ?? false;

    public IReadOnlyList<Product> GetItems()
    {
        if (_session.CurrentUser is not { } user)
        {
            return Array.Empty<Product>();
        }

        return user.WishList
            .Select(id => _repository.Products.FirstOrDefault(product =>
                string.Equals(product.Id, id, StringComparison.OrdinalIgnoreCase)))
            .OfType<Product>()
            .ToList();
    }

    public Result Add(string productId)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        var product = _repository.Products.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, productId, StringComparison.OrdinalIgnoreCase));

        if (product is null)
        {
            return Result.Failure(ErrorMessages.ProductNotFound);
        }

        if (Contains(product.Id))
        {
            return Result.Failure("Produsul este deja în wish list.");
        }

        user.WishList.Add(product.Id);
        _repository.SaveUsers();
        return Result.Success();
    }

    public Result Remove(string productId)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure(ErrorMessages.LoginRequired);
        }

        var removed = user.WishList.RemoveAll(id => string.Equals(id, productId, StringComparison.OrdinalIgnoreCase));
        if (removed == 0)
        {
            return Result.Failure("Produsul nu se află în wish list.");
        }

        _repository.SaveUsers();
        return Result.Success();
    }
}
