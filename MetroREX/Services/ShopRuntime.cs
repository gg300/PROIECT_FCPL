namespace MetroREX.Services;

internal sealed class RuntimeErrorEventArgs : EventArgs
{
    public RuntimeErrorEventArgs(string operation, Exception exception)
    {
        Operation = operation;
        Exception = exception;
    }

    public string Operation { get; }

    public Exception Exception { get; }
}

internal sealed class ShopRuntime
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SyncInterval = TimeSpan.FromSeconds(10);

    private readonly ShopRepository _repository;
    private readonly IClock _clock;
    private readonly CouponService _coupons;
    private readonly PricingService _pricing;
    private readonly InventoryService _inventory;
    private readonly OrderService _orders;
    private readonly ProductSyncService _sync;
    private readonly CancellationTokenSource _cancellation = new();

    private Task? _loop;

    public ShopRuntime(
        ShopRepository repository,
        IClock clock,
        CouponService coupons,
        PricingService pricing,
        InventoryService inventory,
        OrderService orders,
        ProductSyncService sync)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _coupons = coupons ?? throw new ArgumentNullException(nameof(coupons));
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
    }

    public event EventHandler? DayStarted;

    public event EventHandler? ProductsSynchronized;

    public event EventHandler<RuntimeErrorEventArgs>? ErrorOccurred;

    public bool IsRunning => _loop is not null;

    public void Start()
    {
        if (_loop is not null)
        {
            throw new InvalidOperationException("Procesele de fundal sunt deja pornite.");
        }

        if (SynchronizationContext.Current is null)
        {
            throw new InvalidOperationException("Procesele de fundal trebuie pornite din firul interfetei (UI).");
        }

        _clock.Changed += OnClockChanged;
        _loop = LoopAsync(_cancellation.Token);
    }

    public async Task StopAsync()
    {
        if (_loop is null)
        {
            return;
        }

        _clock.Changed -= OnClockChanged;
        _cancellation.Cancel();

        try
        {
            await _loop;
        }
        finally
        {
            _loop = null;
            _repository.Flush();
        }
    }

    public bool RunDailyJobsIfNeeded()
    {
        var today = _clock.Today;
        if (_repository.State.LastDailyRun?.Date == today)
        {
            return false;
        }

        _repository.State.LastDailyRun = today;

        Guard("Cupoane expirate", () => _coupons.RemoveExpired());
        Guard("Cupon zilnic", () => _coupons.GenerateDailyCoupon());
        Guard("Variatia preturilor", () => _pricing.ApplyDailyVariation());
        Guard("Notificari de stoc", () => _inventory.ProcessDayStart());
        Guard("Livrari", () => _orders.ProcessDeliveries());
        Guard("Salvare produse", _repository.SaveProducts);
        Guard("Salvare utilizatori", _repository.SaveUsers);
        Guard("Salvare stare", _repository.SaveState);

        DayStarted?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public async Task SynchronizeProductsAsync()
    {
        try
        {
            await _sync.SynchronizeAsync();
            ProductsSynchronized?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorOccurred?.Invoke(this, new RuntimeErrorEventArgs("Sincronizare Excel", exception));
        }
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        var sinceSync = TimeSpan.Zero;

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                RunDailyJobsIfNeeded();

                sinceSync += TickInterval;
                if (sinceSync >= SyncInterval)
                {
                    sinceSync = TimeSpan.Zero;
                    await SynchronizeProductsAsync();
                }

                Guard("Salvare utilizatori", _repository.Flush);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnClockChanged(object? sender, EventArgs e) => RunDailyJobsIfNeeded();

    private void Guard(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ErrorOccurred?.Invoke(this, new RuntimeErrorEventArgs(operation, exception));
        }
    }
}
