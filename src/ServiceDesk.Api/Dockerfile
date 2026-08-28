FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props global.json ./
COPY src/ServiceDesk.Api/ServiceDesk.Api.csproj src/ServiceDesk.Api/
COPY src/ServiceDesk.Application/ServiceDesk.Application.csproj src/ServiceDesk.Application/
COPY src/ServiceDesk.Domain/ServiceDesk.Domain.csproj src/ServiceDesk.Domain/
COPY src/ServiceDesk.Infrastructure/ServiceDesk.Infrastructure.csproj src/ServiceDesk.Infrastructure/
RUN dotnet restore src/ServiceDesk.Api/ServiceDesk.Api.csproj

COPY src/ServiceDesk.Api/ src/ServiceDesk.Api/
COPY src/ServiceDesk.Application/ src/ServiceDesk.Application/
COPY src/ServiceDesk.Domain/ src/ServiceDesk.Domain/
COPY src/ServiceDesk.Infrastructure/ src/ServiceDesk.Infrastructure/
RUN dotnet publish src/ServiceDesk.Api/ServiceDesk.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ServiceDesk.Api.dll"]