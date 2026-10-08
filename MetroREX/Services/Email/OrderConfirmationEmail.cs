using System.Globalization;
using System.Text;
using MetroREX.Models;

namespace MetroREX.Services.Email;

internal static class OrderConfirmationEmail
{
    private const string DateFormat = "dd-MM-yyyy";

    public static (string Subject, string Body) Compose(Order order, string? maskedCard)
    {
        ArgumentNullException.ThrowIfNull(order);

        var body = new StringBuilder();

        body.AppendLine(Line($"Mulțumim pentru comandă, {order.Profile.Name}!"));
        body.AppendLine();
        body.AppendLine(Line($"Comanda: {order.ShortId}"));
        body.AppendLine(Line($"Data comenzii: {Format(order.OrderDate)}"));
        body.AppendLine(Line($"Data livrării: {Format(order.DeliveryDate)}"));
        body.AppendLine();
        body.AppendLine("Produse:");

        for (var index = 0; index < order.Items.Count; index++)
        {
            var item = order.Items[index];
            body.AppendLine(Line(
                $"  {index + 1}. {item.ProductName} / Preț: {Money.Format(item.UnitPrice)} / Cantitate: {item.Quantity} / Total: {Money.Format(item.Total)}"));
        }

        body.AppendLine();
        body.AppendLine(Line($"Subtotal: {Money.Format(order.Subtotal)}"));

        if (order.DiscountAmount > 0)
        {
            var coupon = string.IsNullOrEmpty(order.CouponCode) ? string.Empty : $" (cupon {order.CouponCode})";
            body.AppendLine(Line($"Reducere{coupon}: -{Money.Format(order.DiscountAmount)}"));
        }

        body.AppendLine(Line($"Total comandă: {Money.Format(order.TotalCost)}"));
        body.AppendLine();
        body.AppendLine(Line($"Adresă de livrare: {order.Profile.Address}"));
        body.AppendLine(Line($"Telefon: {order.Profile.Phone}"));
        body.AppendLine(Line($"Metodă de plată: {DescribePayment(order.PaymentMethod, maskedCard)}"));

        return ($"MetroREX – confirmare comandă {order.ShortId}", body.ToString());
    }

    private static string DescribePayment(PaymentMethod method, string? maskedCard) => method switch
    {
        PaymentMethod.Cash => "numerar la livrare",
        PaymentMethod.Card => string.IsNullOrEmpty(maskedCard) ? "card" : $"card ({maskedCard})",
        _ => method.ToString(),
    };

    private static string Format(DateTime date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string Line(FormattableString text) => FormattableString.Invariant(text);
}
