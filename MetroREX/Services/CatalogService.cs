using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class CatalogService
{
    private readonly ShopRepository _repository;

    public CatalogService(ShopRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public IReadOnlyList<string> GetCategories() => _repository.Products
        .Select(product => product.Category)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(category => category, StringComparer.CurrentCultureIgnoreCase)
        .ToList();

    public IReadOnlyList<Product> GetProducts(string category, PriceSort sort = PriceSort.None)
    {
        var products = _repository.Products
            .Where(product => string.Equals(product.Category, category, StringComparison.OrdinalIgnoreCase));

        IEnumerable<Product> ordered = sort switch
        {
            PriceSort.Ascending => products
                .OrderBy(product => product.CurrentPrice)
                .ThenBy(product => product.Name, StringComparer.CurrentCultureIgnoreCase),
            PriceSort.Descending => products
                .OrderByDescending(product => product.CurrentPrice)
                .ThenBy(product => product.Name, StringComparer.CurrentCultureIgnoreCase),
            _ => products.OrderBy(product => product.Name, StringComparer.CurrentCultureIgnoreCase),
        };

        return ordered.ToList();
    }

    public Product? FindProduct(string productId) => _repository.Products.FirstOrDefault(product =>
        string.Equals(product.Id, productId, StringComparison.OrdinalIgnoreCase));
}
