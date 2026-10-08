using System.Text;
using MetroREX.Models;

namespace MetroREX.Services;

internal sealed class CouponService
{
    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int GroupSize = 3;
    private const int GroupCount = 3;
    private const int MinValidityDays = 1;
    private const int MaxValidityDays = 3;

    private static readonly int[] DiscountSteps = { 5, 10, 15, 20, 25 };

    private readonly ShopRepository _repository;
    private readonly Session _session;
    private readonly IClock _clock;
    private readonly Random _random;

    public CouponService(ShopRepository repository, Session session, IClock clock, Random random)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public IReadOnlyList<Coupon> GetCoupons()
    {
        if (_session.CurrentUser is not { } user)
        {
            return Array.Empty<Coupon>();
        }

        var now = _clock.Now;
        return user.Coupons
            .Where(coupon => !coupon.IsExpired(now))
            .OrderBy(coupon => coupon.ExpirationDate)
            .ThenBy(coupon => coupon.Code, StringComparer.Ordinal)
            .ToList();
    }

    public Result<Coupon> Redeem(string code)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure<Coupon>(ErrorMessages.LoginRequired);
        }

        if (!Coupon.IsValidFormat(code))
        {
            return Result.Failure<Coupon>("Codul trebuie sa aiba forma AAA-BBB-CCC (litere si cifre).");
        }

        var normalized = Coupon.NormalizeCode(code);
        var storeCoupon = _repository.StoreCoupons.FirstOrDefault(coupon => coupon.Code == normalized);

        if (storeCoupon is null)
        {
            return Result.Failure<Coupon>("Cuponul nu exista.");
        }

        if (storeCoupon.IsExpired(_clock.Now))
        {
            return Result.Failure<Coupon>("Cuponul a expirat.");
        }

        if (user.Coupons.Any(coupon => coupon.Code == normalized))
        {
            return Result.Failure<Coupon>("Ai adaugat deja acest cupon.");
        }

        var owned = storeCoupon.Clone();
        user.Coupons.Add(owned);
        _repository.SaveUsers();
        return Result.Success(owned);
    }

    public Coupon? GenerateDailyCoupon()
    {
        var categories = _repository.Products
            .Select(product => product.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (categories.Count == 0)
        {
            return null;
        }

        var coupon = new Coupon
        {
            Code = GenerateUniqueCode(),
            Category = categories[_random.Next(categories.Count)],
            ExpirationDate = _clock.Today.AddDays(_random.Next(MinValidityDays, MaxValidityDays + 1)),
            DiscountPercent = DiscountSteps[_random.Next(DiscountSteps.Length)],
        };

        _repository.StoreCoupons.Add(coupon);
        _repository.SaveCoupons();
        return coupon;
    }

    public int RemoveExpired()
    {
        var now = _clock.Now;

        var removedFromStore = _repository.StoreCoupons.RemoveAll(coupon => coupon.IsExpired(now));
        if (removedFromStore > 0)
        {
            _repository.SaveCoupons();
        }

        var removedFromUsers = _repository.Users.Sum(user => user.Coupons.RemoveAll(coupon => coupon.IsExpired(now)));
        if (removedFromUsers > 0)
        {
            _repository.SaveUsers();
        }

        return removedFromStore + removedFromUsers;
    }

    private string GenerateUniqueCode()
    {
        string code;
        do
        {
            code = GenerateCode();
        }
        while (_repository.StoreCoupons.Any(coupon => coupon.Code == code));

        return code;
    }

    private string GenerateCode()
    {
        var builder = new StringBuilder(GroupSize * GroupCount + GroupCount - 1);

        for (var group = 0; group < GroupCount; group++)
        {
            if (group > 0)
            {
                builder.Append('-');
            }

            for (var index = 0; index < GroupSize; index++)
            {
                builder.Append(CodeAlphabet[_random.Next(CodeAlphabet.Length)]);
            }
        }

        return builder.ToString();
    }
}
