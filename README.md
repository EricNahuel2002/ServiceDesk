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

# Ejecución con Docker

Levanta todo el stack (SQL Server, Azurite, API, Azure Functions y Frontend) en contenedores.

## Requisitos

- Docker Desktop (o Docker Engine con plugin compose).
- No necesitas tener SQL Server ni Azurite instalados en tu PC: corren dentro de contenedores aislados.

## Configuración de `.env`

1. Copia la plantilla:
   ```bash
   cp .env.example .env
   ```
2. Completa las variables obligatorias:
   | Variable | Descripción |
   | --- | --- |
   | `SA_PASSWORD` | Contraseña del usuario `sa` de SQL Server (mín. 8 caracteres, mezclando mayúsculas, minúsculas y números/símbolos). |
   | `JWT_SECRET` | Clave para firmar los JWT (al menos 32 caracteres). Debe coincidir con la usada por la app. |

3. Opcionales:
   | Variable | Descripción |
   | --- | --- |
   | `SQL_PORT` | Puerto del SQL Server en el host (por defecto `14333`). |
   | `API_PORT` | Puerto de la API en el host (por defecto `5216`). |
   | `FRONTEND_PORT` | Puerto del frontend en el host (por defecto `5173`). |
   | `AZURITE_BLOB_PORT` / `AZURITE_QUEUE_PORT` / `AZURITE_TABLE_PORT` | Puertos de Azurite (por defecto `10000`/`10001`/`10002`). |
   | `ACS_ENABLED`, `ACS_CONNECTION_STRING`, `ACS_SENDER_ADDRESS`, `ACS_SENDER_DISPLAY_NAME` | Habilita el envío de emails reales desde las Azure Functions mediante Azure Communication Services. Si no se configuran, las Functions arrancan con el email deshabilitado. |

## Comandos

```bash
# Construir y levantar el stack en segundo plano
docker compose up --build -d

# Ver los logs de un servicio (ej. api)
docker compose logs -f api

# Ver el estado y los healthchecks
docker compose ps

# Detener los contenedores (sin borrar los datos)
docker compose down

# Detener y borrar contenedores + volúmenes (pierde la base de datos local de Docker)
docker compose down -v
```

## URLs

| Servicio | URL local |
| --- | --- |
| Frontend | `http://localhost:5173` |
| API (Scalar/OpenAPI) | `http://localhost:5216` |
| Health check de la API | `http://localhost:5216/health` |
| SQL Server | `localhost,14333` (usuario `sa`) |
| Azurite (Blob/Queue/Table) | `http://localhost:10000` / `10001` / `10002` |

El frontend redirige `/api` y `/hubs` hacia la API mediante el proxy de Vite, así que las peticiones al navegador son same-origin. La base de datos se migra y se siembra automáticamente al arrancar la API.
