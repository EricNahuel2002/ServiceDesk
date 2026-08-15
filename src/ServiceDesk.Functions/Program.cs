using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ServiceDesk.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((_, configuration) =>
    {
        configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: false);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddNotificationServices(context.Configuration);
    })
    .Build();

await host.RunAsync();
