namespace MetroREX.Services;

internal sealed class InventoryService
{
    private readonly ShopRepository _repository;
    private readonly NotificationService _notifications;

    public InventoryService(ShopRepository repository, NotificationService notifications)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    public int ProcessDayStart()
    {
        var restocked = 0;

        foreach (var product in _repository.Products)
        {
            if (product.QuantityYesterday == 0 && product.Quantity > 0)
            {
                _notifications.NotifyBackInStock(product);
                restocked++;
            }

            product.QuantityYesterday = product.Quantity;
        }

        return restocked;
    }
}
