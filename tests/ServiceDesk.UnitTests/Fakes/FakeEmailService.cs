using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeEmailService : IEmailService
{
    private readonly List<SentEmail> _sent = [];

    public IReadOnlyList<SentEmail> Sent => _sent;

    public bool ThrowOnSend { get; set; }

    public Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend)
        {
            throw new InvalidOperationException("Fallo simulado al enviar el email.");
        }

        _sent.Add(new SentEmail(toEmail, subject, body));

        return Task.CompletedTask;
    }

    internal sealed record SentEmail(string ToEmail, string Subject, string Body);
}
