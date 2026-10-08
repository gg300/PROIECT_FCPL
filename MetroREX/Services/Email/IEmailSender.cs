namespace MetroREX.Services.Email;

internal interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}

internal sealed class UnconfiguredEmailSender : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default) =>
        Task.FromException(new InvalidOperationException(
            "Trimiterea e-mailurilor nu este configurata (lipseste fisierul Storage/email.json)."));
}
