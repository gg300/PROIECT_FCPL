using System.Text.RegularExpressions;

namespace MetroREX.Models;

public class Coupon
{
    private static readonly Regex CodeFormat = new(@"^[A-Z0-9]{3}-[A-Z0-9]{3}-[A-Z0-9]{3}$", RegexOptions.Compiled);

    public string Code { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime ExpirationDate { get; set; }

    public int DiscountPercent { get; set; }

    public string Details => $"-{DiscountPercent}% la categoria {Category}";

    public bool IsExpired(DateTime now) => now.Date > ExpirationDate.Date;

    public static bool IsValidFormat(string? code) =>
        !string.IsNullOrWhiteSpace(code) && CodeFormat.IsMatch(code.Trim().ToUpperInvariant());
}
