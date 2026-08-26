# ServiceDesk
[![CI](https://github.com/EricNahuel2002/ServiceDesk/actions/workflows/ci.yml/badge.svg)](https://github.com/EricNahuel2002/ServiceDesk/actions/workflows/ci.yml)

ServiceDesk es una plataforma SaaS para gestionar incidencias, mantenimiento y soporte técnico de empresas.

# Objetivo

Desarrollar una API REST empresarial en ASP.NET Core utilizando servicios de Azure y siguiendo una arquitectura escalable.

# Tecnologías

Backend

*  ASP.NET Core Web API
-  Entity Framework Core
*  SQL Server
*  JWT
*  FluentValidation
*  AutoMapper
*  Serilog
*  xUnit

Azure

*  App Service
*  Azure SQL Database
*  Blob Storage
*  Key Vault
*  Application Insights
*  Queue Storage
*  Azure Functions
*  GitHub Actions

# Ejecución Local

Para levantar el proyecto localmente, sigue estos pasos:

1. **Restaurar dependencias:**
   ```bash
   dotnet restore
   ```

2. **Construir el proyecto:**
   ```bash
   dotnet build
   ```

3. **Ejecutar la API:**
   ```bash
   dotnet run --project src/ServiceDesk.Api/ServiceDesk.Api.csproj
   ```
   La API estará disponible en `https://localhost:5001` y `http://localhost:5000`

4. **Configurar la base de datos:**
   - Asegúrate de tener SQL Server disponible
   - Actualiza la cadena de conexión en `appsettings.Development.json`
   - Aplica las migraciones:
     ```bash
     dotnet ef database update
     ```

5. **Variables de entorno:**
   - Copia `appsettings.example.json` a `appsettings.Development.json`
   - Completa las credenciales necesarias (JWT, Azure Key Vault, etc.)
