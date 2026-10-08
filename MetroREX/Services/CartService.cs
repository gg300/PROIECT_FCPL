using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class CartService
{
    private readonly ShopRepository _repository;
    private readonly Session _session;

    public CartService(ShopRepository repository, Session session)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public Cart Current => _session.Cart;

    public int AvailableQuantity(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var inCart = Current.Items
            .FirstOrDefault(item => string.Equals(item.ProductId, product.Id, StringComparison.OrdinalIgnoreCase))
            ?.Quantity ?? 0;

        return Math.Max(0, product.Quantity - inCart);
    }

    public Result Add(string productId, int quantity)
    {
        if (quantity < 1)
        {
            return Result.Failure("Cantitatea trebuie să fie cel puțin 1.");
        }

        var product = FindProduct(productId);
        if (product is null)
        {
            return Result.Failure(ErrorMessages.ProductNotFound);
        }

        var available = AvailableQuantity(product);
        if (quantity > available)
        {
            return Result.Failure(available == 0
                ? "Nu mai există stoc disponibil pentru acest produs."
                : $"Sunt disponibile doar {available} bucăți.");
        }

        Current.Add(product, quantity);
        return Result.Success();
    }

    public Result SetQuantity(string productId, int quantity)
    {
        var item = Current.Items.FirstOrDefault(candidate =>
            string.Equals(candidate.ProductId, productId, StringComparison.OrdinalIgnoreCase));

        if (item is null)
        {
            return Result.Failure("Produsul nu se află în coș.");
        }

        if (quantity < 1)
        {
            return Result.Failure("Cantitatea trebuie să fie cel puțin 1.");
        }

        var product = FindProduct(productId);
        if (product is null)
        {
            return Result.Failure(ErrorMessages.ProductNotFound);
        }

        if (quantity > product.Quantity)
        {
            return Result.Failure($"Sunt disponibile doar {product.Quantity} bucăți.");
        }

        item.Quantity = quantity;
        item.UnitPrice = product.CurrentPrice;
        return Result.Success();
    }

    public void Remove(string productId) => Current.Remove(productId);

    public void Clear() => Current.Clear();

    private Product? FindProduct(string productId) => _repository.Products.FirstOrDefault(product =>
        string.Equals(product.Id, productId, StringComparison.OrdinalIgnoreCase));
}
