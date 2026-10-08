using MetroREX.Data;
using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class ShopRepository
{
    private readonly IStore<User> _userStore;
    private readonly IStore<Product> _productStore;
    private readonly IStore<Coupon> _couponStore;
    private readonly IStore<AppState> _stateStore;
    private readonly object _productWriteGate = new();

    private long _productsRevision;
    private long _writtenProductsRevision;
    private bool _usersDirty;

    public ShopRepository(
        IStore<User> userStore,
        IStore<Product> productStore,
        IStore<Coupon> couponStore,
        IStore<AppState> stateStore)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _productStore = productStore ?? throw new ArgumentNullException(nameof(productStore));
        _couponStore = couponStore ?? throw new ArgumentNullException(nameof(couponStore));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    public List<User> Users { get; } = new();

    public List<Product> Products { get; } = new();

    public List<Coupon> StoreCoupons { get; } = new();

    public AppState State { get; private set; } = new();

    public long ProductsRevision => Interlocked.Read(ref _productsRevision);

    public void Load() =>
        Apply(_userStore.Load(), _productStore.Load(), _couponStore.Load(), _stateStore.Load());

    public async Task LoadAsync()
    {
        var users = Task.Run(_userStore.Load);
        var products = Task.Run(_productStore.Load);
        var coupons = Task.Run(_couponStore.Load);
        var states = Task.Run(_stateStore.Load);

        await Task.WhenAll(users, products, coupons, states);
        Apply(users.Result, products.Result, coupons.Result, states.Result);
    }

    public void SaveUsers()
    {
        _usersDirty = false;
        _userStore.Save(Users);
    }

    public void QueueUsersSave() => _usersDirty = true;

    public void Flush()
    {
        if (_usersDirty)
        {
            SaveUsers();
        }
    }

    public void SaveCoupons() => _couponStore.Save(StoreCoupons);

    public void SaveState() => _stateStore.Save(new[] { State });

    public void SaveProducts()
    {
        var revision = Interlocked.Increment(ref _productsRevision);
        WriteProducts(CloneProducts(), revision);
    }

    public Task SaveProductsAsync()
    {
        var revision = ProductsRevision;
        var snapshot = CloneProducts();
        return Task.Run(() => WriteProducts(snapshot, revision));
    }

    public Task<List<Product>> ReadProductsAsync() => Task.Run(_productStore.Load);

    private void Apply(List<User> users, List<Product> products, List<Coupon> coupons, List<AppState> states)
    {
        Users.Clear();
        Users.AddRange(users);

        Products.Clear();
        Products.AddRange(products);

        StoreCoupons.Clear();
        StoreCoupons.AddRange(coupons);

        State = states.FirstOrDefault() ?? new AppState();

        lock (_productWriteGate)
        {
            _writtenProductsRevision = ProductsRevision;
        }
    }

    private List<Product> CloneProducts() => Products.Select(product => product.Clone()).ToList();

    private void WriteProducts(List<Product> snapshot, long revision)
    {
        lock (_productWriteGate)
        {
            if (revision < _writtenProductsRevision)
            {
                return;
            }

            _productStore.Save(snapshot);
            _writtenProductsRevision = revision;
        }
    }
}
