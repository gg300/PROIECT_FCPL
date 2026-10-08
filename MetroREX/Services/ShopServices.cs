using MetroREX.Data;
using MetroREX.Models;
using MetroREX.Services.Email;

namespace MetroREX.Services;

internal sealed class ShopServices
{
    internal ShopServices(ShopRepository repository, IEmailSender emailSender, Clock clock, Random random)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(emailSender);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(random);

        Repository = repository;
        Clock = clock;
        Session = new Session();
        Notifications = new NotificationService(repository, clock);
        Auth = new AuthService(repository, Session);
        Catalog = new CatalogService(repository);
        Cart = new CartService(repository, Session);
        WishList = new WishListService(repository, Session);
        Coupons = new CouponService(repository, Session, clock, random);
        Orders = new OrderService(repository, Session, clock, Notifications);
        Checkout = new CheckoutService(repository, Session, clock, emailSender, random);

        var pricing = new PricingService(repository, Notifications, random);
        var inventory = new InventoryService(repository, Notifications);
        var sync = new ProductSyncService(repository, Notifications);

        Runtime = new ShopRuntime(repository, clock, Coupons, pricing, inventory, Orders, sync);
    }

    internal ShopRepository Repository { get; }

    public Clock Clock { get; }

    public Session Session { get; }

    public NotificationService Notifications { get; }

    public AuthService Auth { get; }

    public CatalogService Catalog { get; }

    public CartService Cart { get; }

    public WishListService WishList { get; }

    public CouponService Coupons { get; }

    public OrderService Orders { get; }

    public CheckoutService Checkout { get; }

    public ShopRuntime Runtime { get; }

    public static ShopServices Create()
    {
        var repository = new ShopRepository(
            new JsonStore<User>(AppPaths.UsersFile),
            new ExcelProductStore(AppPaths.ProductsFile),
            new CouponFileStore(AppPaths.CouponsFile),
            new JsonStore<AppState>(AppPaths.StateFile));

        repository.Load();

        var settings = SmtpSettings.Load(AppPaths.EmailSettingsFile);
        IEmailSender emailSender = settings is null ? new UnconfiguredEmailSender() : new SmtpEmailSender(settings);

        return new ShopServices(repository, emailSender, new Clock(), Random.Shared);
    }
}
