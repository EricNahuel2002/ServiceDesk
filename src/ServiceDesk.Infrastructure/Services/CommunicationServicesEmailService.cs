using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.Configuration;

namespace ServiceDesk.Infrastructure.Services;

public sealed class CommunicationServicesEmailService : IEmailService
{
    private readonly CommunicationServicesSettings _settings;
    private readonly ILogger<CommunicationServicesEmailService> _logger;
    private readonly EmailClient? _client;

    public CommunicationServicesEmailService(
        IOptions<CommunicationServicesSettings> settings,
        ILogger<CommunicationServicesEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (_settings.Enabled)
        {
            _client = new EmailClient(_settings.ConnectionString);
        }
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
                "Communication Services email deshabilitado, se omitió el envío a {ToEmail} con asunto {Subject}",
                toEmail,
                subject);

            return;
        }

        if (_client is null)
        {
            throw new InvalidOperationException(
                "El cliente de email no fue inicializado porque la configuración no es válida.");
        }

        EmailMessage message = new(
            _settings.SenderAddress,
            toEmail,
            new EmailContent(subject) { PlainText = body });

        EmailSendOperation operation = await _client.SendAsync(
            WaitUntil.Completed,
            message,
            cancellationToken);

        if (operation.Value.Status != EmailSendStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"El envío de email a {toEmail} finalizó con estado {operation.Value.Status}.");
        }

        _logger.LogInformation(
            "Email enviado a {ToEmail} con asunto {Subject} mediante Communication Services",
            toEmail,
            subject);
    }
}
