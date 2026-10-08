using System.Text.RegularExpressions;

namespace MetroREX.Models;

/// <summary>Cupon de forma AAA-BBB-CCC (litere mari și cifre), valabil pentru o categorie.</summary>
public class Coupon
{
    private static readonly Regex CodeFormat = new(@"^[A-Z0-9]{3}-[A-Z0-9]{3}-[A-Z0-9]{3}$", RegexOptions.Compiled);

    public string Code { get; set; } = "";
    public string Category { get; set; } = "";
    public DateTime ExpirationDate { get; set; }

    /// <summary>Reducerea în procente (ex: 10 = 10%).</summary>
    public int DiscountPercent { get; set; }

    /// <summary>Text pentru ecranul „Cupoanele mele”.</summary>
    public string Details => $"-{DiscountPercent}% la categoria {Category}";

    public bool IsExpired(DateTime now) => now.Date > ExpirationDate.Date;

    public static bool IsValidFormat(string? code) =>
        !string.IsNullOrWhiteSpace(code) && CodeFormat.IsMatch(code.Trim().ToUpperInvariant());
}
