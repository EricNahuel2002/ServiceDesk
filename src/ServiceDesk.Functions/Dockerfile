FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/ServiceDesk.Functions/ServiceDesk.Functions.csproj src/ServiceDesk.Functions/
COPY src/ServiceDesk.Application/ServiceDesk.Application.csproj src/ServiceDesk.Application/
COPY src/ServiceDesk.Domain/ServiceDesk.Domain.csproj src/ServiceDesk.Domain/
COPY src/ServiceDesk.Infrastructure/ServiceDesk.Infrastructure.csproj src/ServiceDesk.Infrastructure/
RUN dotnet restore src/ServiceDesk.Functions/ServiceDesk.Functions.csproj

COPY src/ServiceDesk.Functions/ src/ServiceDesk.Functions/
COPY src/ServiceDesk.Application/ src/ServiceDesk.Application/
COPY src/ServiceDesk.Domain/ src/ServiceDesk.Domain/
COPY src/ServiceDesk.Infrastructure/ src/ServiceDesk.Infrastructure/
RUN dotnet publish src/ServiceDesk.Functions/ServiceDesk.Functions.csproj -c Release -o /home/site/wwwroot --no-restore

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
COPY --from=build ["/home/site/wwwroot", "/home/site/wwwroot"]