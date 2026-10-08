namespace MetroREX.Models;

public sealed class CardDetails
{
    public string Holder { get; init; } = "";
    public string Number { get; init; } = "";
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public string Cvv { get; init; } = "";

    public string MaskedNumber
    {
        get
        {
            var digits = new string(Number.Where(char.IsAsciiDigit).ToArray());
            return digits.Length < 4 ? "****" : $"**** **** **** {digits[^4..]}";
        }
    }

    public override string ToString() => MaskedNumber;
}
