using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class ProductSyncService
{
    private readonly ShopRepository _repository;
    private readonly NotificationService _notifications;

    public ProductSyncService(ShopRepository repository, NotificationService notifications)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    public async Task SynchronizeAsync()
    {
        var revision = _repository.ProductsRevision;
        var external = await _repository.ReadProductsAsync();

        if (external.Count > 0 && revision == _repository.ProductsRevision)
        {
            Merge(external);
        }

        await _repository.SaveProductsAsync();
    }

    private void Merge(List<Product> external)
    {
        var current = _repository.Products.ToDictionary(product => product.Id, StringComparer.OrdinalIgnoreCase);
        var externalIds = new HashSet<string>(external.Select(product => product.Id), StringComparer.OrdinalIgnoreCase);

        _repository.Products.RemoveAll(product => !externalIds.Contains(product.Id));

        foreach (var incoming in external)
        {
            if (!current.TryGetValue(incoming.Id, out var existing))
            {
                _repository.Products.Add(incoming);
                continue;
            }

            var previousPrice = existing.CurrentPrice;
            existing.CopyFrom(incoming);

            if (existing.CurrentPrice != previousPrice)
            {
                _notifications.NotifyPriceChange(existing, previousPrice, existing.CurrentPrice);
            }
        }
    }
}
