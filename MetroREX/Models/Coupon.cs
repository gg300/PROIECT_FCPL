using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace MetroREX.Models;

public class Coupon
{
    private static readonly Regex CodeFormat = new(@"^[A-Z0-9]{3}-[A-Z0-9]{3}-[A-Z0-9]{3}$", RegexOptions.Compiled);

    public string Code { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime ExpirationDate { get; set; }

    public int DiscountPercent { get; set; }

    [JsonIgnore]
    public string Details => $"-{DiscountPercent}% la categoria {Category}";

    public bool IsExpired(DateTime now) => now.Date > ExpirationDate.Date;

    public Coupon Clone() => new()
    {
        Code = Code,
        Category = Category,
        ExpirationDate = ExpirationDate,
        DiscountPercent = DiscountPercent,
    };

    public static string NormalizeCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static bool IsValidFormat(string? code) =>
        !string.IsNullOrWhiteSpace(code) && CodeFormat.IsMatch(NormalizeCode(code));
}
