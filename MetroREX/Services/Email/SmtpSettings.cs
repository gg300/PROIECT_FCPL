using System.Text.Json;

namespace MetroREX.Services.Email;

internal sealed class SmtpSettings
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    public string Username { get; set; } = "";

    public string Password { get; set; } = "";

    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "MetroREX";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Host)
        && Port is > 0 and <= 65535
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FromAddress);

    public static SmtpSettings? Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<SmtpSettings>(File.ReadAllText(path), Options);
            return settings is { IsComplete: true } ? settings : null;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Fisierul '{path}' nu contine setari de e-mail valide.", exception);
        }
    }
}
