using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Configuration;

namespace ServiceDesk.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "Email service disabled, skipping email to {ToEmail} with subject {Subject}",
                toEmail,
                subject);

            return;
        }

        MimeMessage message = BuildMessage(toEmail, subject, body);

        try
        {
            using SmtpClient client = new();
            SecureSocketOptions socketOptions =
                _settings.EnableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName)
                && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {ToEmail} with subject {Subject}", toEmail, subject);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to send email to {ToEmail} with subject {Subject}",
                toEmail,
                subject);
        }
    }

    private MimeMessage BuildMessage(string toEmail, string subject, string body)
    {
        MimeMessage message = new();
        message.From.Add(new MailboxAddress(_settings.FromDisplayName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(string.Empty, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };

        return message;
    }
}
