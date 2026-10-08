using System.Net.Mail;
using System.Text.RegularExpressions;
using MetroREX.Models;

namespace MetroREX.Services;

internal static class Validation
{
    private const int MinPasswordLength = 8;
    private const int MaxEmailLength = 254;
    private const int MinYear = 2000;
    private const int MaxYear = 2100;

    private static readonly Regex UsernamePattern = new(@"^[A-Za-z0-9._-]{3,20}$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9]{7,15}$", RegexOptions.Compiled);
    private static readonly char[] PhoneSeparators = { ' ', '-', '.', '(', ')' };
    private static readonly char[] CardSeparators = { ' ', '-' };

    public static string? Username(string? value) =>
        value is not null && UsernamePattern.IsMatch(value.Trim())
            ? null
            : "Numele de utilizator trebuie să aibă 3–20 de caractere (litere, cifre, „.”, „_” sau „-”).";

    public static string? Password(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < MinPasswordLength)
        {
            return $"Parola trebuie să aibă cel puțin {MinPasswordLength} caractere.";
        }

        return value.Any(char.IsLetter) && value.Any(char.IsDigit)
            ? null
            : "Parola trebuie să conțină cel puțin o literă și o cifră.";
    }

    public static string? Profile(UserProfile? profile)
    {
        if (profile is null)
        {
            return "Profilul lipsește.";
        }

        return Name(profile.Name) ?? Address(profile.Address) ?? Email(profile.Email) ?? Phone(profile.Phone);
    }

    public static string? Name(string? value)
    {
        var length = value?.Trim().Length ?? 0;
        return length is >= 2 and <= 100 ? null : "Numele trebuie să aibă între 2 și 100 de caractere.";
    }

    public static string? Address(string? value)
    {
        var length = value?.Trim().Length ?? 0;
        return length is >= 5 and <= 200 ? null : "Adresa trebuie să aibă între 5 și 200 de caractere.";
    }

    public static string? Email(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        var isValid = trimmed.Length is > 0 and <= MaxEmailLength
            && MailAddress.TryCreate(trimmed, out var address)
            && string.Equals(address.Address, trimmed, StringComparison.Ordinal)
            && address.Host.Contains('.');

        return isValid ? null : "Adresa de e-mail nu este validă.";
    }

    public static string? Phone(string? value)
    {
        var normalized = NormalizePhone(value);
        return PhonePattern.IsMatch(normalized) ? null : "Numărul de telefon nu este valid (7–15 cifre).";
    }

    public static string NormalizePhone(string? value) =>
        string.Concat((value ?? string.Empty).Trim().Split(PhoneSeparators));

    public static bool TryParseExpiry(string? text, out int month, out int year)
    {
        month = 0;
        year = 0;

        var parts = (text ?? string.Empty).Trim().Split('/');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out month)
            || !int.TryParse(parts[1], out var rawYear)
            || month is < 1 or > 12)
        {
            return false;
        }

        year = rawYear < 100 ? 2000 + rawYear : rawYear;
        return year is >= MinYear and <= MaxYear;
    }

    public static string? Card(CardDetails? card, DateTime today)
    {
        if (card is null)
        {
            return "Datele cardului lipsesc.";
        }

        if (card.Holder.Trim().Length < 3)
        {
            return "Numele de pe card nu este valid.";
        }

        var number = string.Concat(card.Number.Trim().Split(CardSeparators));
        if (number.Length is < 13 or > 19 || !number.All(char.IsAsciiDigit) || !PassesLuhn(number))
        {
            return "Numărul cardului nu este valid.";
        }

        if (!IsExpiryValid(card.ExpiryMonth, card.ExpiryYear, today))
        {
            return "Cardul este expirat sau data de expirare nu este validă.";
        }

        return card.Cvv.Length is 3 or 4 && card.Cvv.All(char.IsAsciiDigit) ? null : "Codul CVV nu este valid.";
    }

    private static bool IsExpiryValid(int month, int year, DateTime today)
    {
        if (month is < 1 or > 12 || year is < MinYear or > MaxYear)
        {
            return false;
        }

        return new DateTime(year, month, 1).AddMonths(1) > today.Date;
    }

    private static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var doubleIt = false;

        for (var index = digits.Length - 1; index >= 0; index--)
        {
            var digit = digits[index] - '0';

            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }
}
