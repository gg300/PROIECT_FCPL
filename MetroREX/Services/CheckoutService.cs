using System.Net.Mail;
using MetroREX.Models;
using MetroREX.Services.Email;

namespace MetroREX.Services;

internal sealed class CheckoutService
{
    private const int MinDeliveryDays = 1;
    private const int MaxDeliveryDays = 5;

    private readonly ShopRepository _repository;
    private readonly Session _session;
    private readonly IClock _clock;
    private readonly IEmailSender _emailSender;
    private readonly Random _random;

    public CheckoutService(
        ShopRepository repository,
        Session session,
        IClock clock,
        IEmailSender emailSender,
        Random random)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public Result<OrderQuote> Quote(string? couponCode = null)
    {
        var cart = _session.Cart;
        if (cart.IsEmpty)
        {
            return Result.Failure<OrderQuote>("Cosul este gol.");
        }

        var lines = new List<(OrderItem Item, Product Product)>();

        foreach (var cartItem in cart.Items)
        {
            var product = _repository.Products.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, cartItem.ProductId, StringComparison.OrdinalIgnoreCase));

            if (product is null)
            {
                return Result.Failure<OrderQuote>($"Produsul „{cartItem.ProductName}” nu mai este in magazin.");
            }

            if (cartItem.Quantity > product.Quantity)
            {
                return Result.Failure<OrderQuote>(
                    $"Din „{product.Name}” sunt disponibile doar {product.Quantity} bucati.");
            }

            lines.Add((
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.CurrentPrice,
                    Quantity = cartItem.Quantity,
                },
                product));
        }

        var subtotal = lines.Sum(line => line.Item.Total);

        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return Result.Success(new OrderQuote(lines.Select(line => line.Item).ToList(), null, subtotal, 0m));
        }

        var couponResult = ResolveCoupon(couponCode, lines.Select(line => line.Product.Category));
        if (!couponResult.IsSuccess)
        {
            return Result.Failure<OrderQuote>(couponResult.Error);
        }

        var coupon = couponResult.Value;
        var eligibleTotal = lines
            .Where(line => string.Equals(line.Product.Category, coupon.Category, StringComparison.OrdinalIgnoreCase))
            .Sum(line => line.Item.Total);

        var discount = Math.Round(eligibleTotal * coupon.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return Result.Success(new OrderQuote(lines.Select(line => line.Item).ToList(), coupon, subtotal, discount));
    }

    public async Task<Result<CheckoutOutcome>> PlaceOrderAsync(CheckoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = _session.CurrentUser;

        var profileResult = ResolveProfile(user, request);
        if (!profileResult.IsSuccess)
        {
            return Result.Failure<CheckoutOutcome>(profileResult.Error);
        }

        if (request.PaymentMethod == PaymentMethod.Card)
        {
            var cardError = Validation.Card(request.Card, _clock.Today);
            if (cardError is not null)
            {
                return Result.Failure<CheckoutOutcome>(cardError);
            }
        }

        var quoteResult = Quote(request.CouponCode);
        if (!quoteResult.IsSuccess)
        {
            return Result.Failure<CheckoutOutcome>(quoteResult.Error);
        }

        var order = Commit(user, quoteResult.Value, profileResult.Value, request.PaymentMethod);

        var cardNumber = request.PaymentMethod == PaymentMethod.Card ? request.Card?.MaskedNumber : null;
        var (emailSent, emailError) = await SendConfirmationAsync(order, cardNumber);

        return Result.Success(new CheckoutOutcome(order, emailSent, emailError));
    }

    private Result<UserProfile> ResolveProfile(User? user, CheckoutRequest request)
    {
        if (user is not null)
        {
            return Result.Success(user.Profile.Clone());
        }

        var error = Validation.Profile(request.GuestProfile);
        return error is null
            ? Result.Success(request.GuestProfile!.Normalized())
            : Result.Failure<UserProfile>(error);
    }

    private Result<Coupon> ResolveCoupon(string code, IEnumerable<string> cartCategories)
    {
        if (_session.CurrentUser is not { } user)
        {
            return Result.Failure<Coupon>("Cupoanele pot fi folosite doar de utilizatorii autentificati.");
        }

        var normalized = Coupon.NormalizeCode(code);
        var coupon = user.Coupons.FirstOrDefault(candidate => candidate.Code == normalized);

        if (coupon is null)
        {
            return Result.Failure<Coupon>("Nu ai acest cupon in lista ta.");
        }

        if (coupon.IsExpired(_clock.Now))
        {
            return Result.Failure<Coupon>("Cuponul a expirat.");
        }

        var appliesToCart = cartCategories.Any(category =>
            string.Equals(category, coupon.Category, StringComparison.OrdinalIgnoreCase));

        return appliesToCart
            ? Result.Success(coupon)
            : Result.Failure<Coupon>($"Cuponul este valabil doar pentru categoria „{coupon.Category}”.");
    }

    private Order Commit(User? user, OrderQuote quote, UserProfile profile, PaymentMethod paymentMethod)
    {
        var order = new Order
        {
            OrderDate = _clock.Now,
            DeliveryDate = _clock.Today.AddDays(_random.Next(MinDeliveryDays, MaxDeliveryDays + 1)),
            Status = OrderStatus.Active,
            Items = quote.Items.ToList(),
            PaymentMethod = paymentMethod,
            Profile = profile,
            CouponCode = quote.Coupon?.Code,
            DiscountAmount = quote.Discount,
        };

        foreach (var item in order.Items)
        {
            var product = _repository.Products.First(candidate =>
                string.Equals(candidate.Id, item.ProductId, StringComparison.OrdinalIgnoreCase));
            product.Quantity -= item.Quantity;
        }

        _session.Cart.Clear();
        _repository.SaveProducts();

        if (user is not null)
        {
            user.Orders.Add(order);

            if (quote.Coupon is not null)
            {
                user.Coupons.RemoveAll(coupon => coupon.Code == quote.Coupon.Code);
            }

            _repository.SaveUsers();
        }

        return order;
    }

    private async Task<(bool Sent, string? Error)> SendConfirmationAsync(Order order, string? maskedCard)
    {
        try
        {
            var (subject, body) = OrderConfirmationEmail.Compose(order, maskedCard);
            await _emailSender.SendAsync(order.Profile.Email, subject, body);
            return (true, null);
        }
        catch (Exception exception) when (exception is SmtpException or InvalidOperationException or IOException or FormatException)
        {
            return (false, exception.Message);
        }
    }
}
