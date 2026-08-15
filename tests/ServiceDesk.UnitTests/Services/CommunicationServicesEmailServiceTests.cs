using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ServiceDesk.Application.Configuration;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.UnitTests.Services;

public sealed class CommunicationServicesEmailServiceTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_DoesNotThrow()
    {
        CommunicationServicesEmailService service = CreateService(disabled: true);

        await service.SendAsync(
            "destinatario@servicedesk.local",
            "Asunto de prueba",
            "Cuerpo de prueba");
    }

    private static CommunicationServicesEmailService CreateService(bool disabled)
    {
        CommunicationServicesSettings settings = new()
        {
            Enabled = !disabled
        };

        return new CommunicationServicesEmailService(
            Options.Create(settings),
            NullLogger<CommunicationServicesEmailService>.Instance);
    }
}
