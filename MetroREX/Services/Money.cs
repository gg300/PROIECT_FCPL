using System.Globalization;

namespace MetroREX.Services;

internal static class Money
{
    public static string Format(decimal amount) =>
        string.Create(CultureInfo.InvariantCulture, $"{amount:0.##}$");
}
