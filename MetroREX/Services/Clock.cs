namespace MetroREX.Services;

internal interface IClock
{
    event EventHandler? Changed;

    DateTime Now { get; }

    DateTime Today { get; }
}

internal sealed class Clock : IClock
{
    private readonly object _gate = new();
    private TimeSpan _offset = TimeSpan.Zero;

    public event EventHandler? Changed;

    public DateTime Now
    {
        get
        {
            lock (_gate)
            {
                return DateTime.Now + _offset;
            }
        }
    }

    public DateTime Today => Now.Date;

    public bool IsOverridden
    {
        get
        {
            lock (_gate)
            {
                return _offset != TimeSpan.Zero;
            }
        }
    }

    public void SetNow(DateTime value)
    {
        lock (_gate)
        {
            _offset = value - DateTime.Now;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetDate(DateTime date) => SetNow(date.Date + Now.TimeOfDay);

    public void Advance(TimeSpan amount) => SetNow(Now + amount);

    public void AdvanceDays(int days) => Advance(TimeSpan.FromDays(days));

    public void Reset()
    {
        lock (_gate)
        {
            _offset = TimeSpan.Zero;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
