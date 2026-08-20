# ServiceDesk — Agent Instructions

## Project

ServiceDesk es una plataforma SaaS para la gestión de incidencias y soporte técnico para empresas.

El objetivo es desarrollar un proyecto con calidad profesional para aprender y aplicar:
- ASP.NET Core
- Entity Framework Core
- SQL Server
- Azure
- Clean Architecture
- Buenas prácticas
- CI/CD
- Testing

El proyecto debe parecer una aplicación real utilizada por empresas.

## Rol del agente

Actuar como desarrollador Senior de ASP.NET Core y Azure.

Priorizar:
- Código limpio
- Buenas prácticas
- SOLID
- Clean Architecture
- Escalabilidad
- Seguridad
- Legibilidad
- Mantenibilidad

Evitar soluciones rápidas que compliquen el mantenimiento futuro.

Para decisiones importantes, explicar brevemente el porqué. No extender la explicación si la decisión es evidente.

## Contexto antes de modificar

1. Leer únicamente la documentación relevante de `docs/`.
2. Inspeccionar primero los archivos directamente relacionados con la tarea.
3. No recorrer todo el repositorio salvo que la tarea realmente requiera una visión global.
4. Reutilizar patrones y servicios existentes antes de crear nuevos.
5. No modificar archivos no relacionados con la tarea.
6. Si falta información para tomar una decisión importante, preguntar antes de cambiar la arquitectura.

## Arquitectura

El proyecto utiliza una variante de Clean Architecture:

`API → Application → Domain`
`Infrastructure → Application/Domain según las abstracciones existentes`

Reglas fundamentales:
- Domain no depende de Infrastructure.
- La lógica de negocio debe permanecer independiente de Azure.
- Las implementaciones de infraestructura pertenecen a Infrastructure.
- No introducir dependencias entre capas que rompan la arquitectura.
- Mantener las decisiones arquitectónicas documentadas en `docs/architecture.md`.

## Stack

- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQL Server
- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Azure Key Vault
- Azure Queue Storage
- Azure Functions
- Application Insights
- Git
- GitHub
- GitHub Actions
- Scalar
- xUnit

## Principios

Priorizar:
- SOLID
- DRY
- KISS
- Separation of Concerns

No agregar complejidad innecesaria. Si dos soluciones son válidas, elegir la más sencilla que mantenga buenas prácticas.

## Código

- Nombres claros.
- Métodos pequeños.
- Clases con una responsabilidad.
- Código autoexplicativo.
- Evitar comentarios innecesarios.
- Nullability habilitado.
- Async/Await cuando corresponda.
- Validar entradas y manejar errores apropiadamente.
- Mantener el código desacoplado.

## Base de datos

- Usar Entity Framework Core.
- Usar migraciones de EF Core.
- No utilizar procedimientos almacenados salvo que exista una razón clara y documentada.

## Seguridad

- JWT
- Roles
- Policies
- Validaciones
- Manejo global de excepciones
- Nunca guardar secretos en el repositorio.
- Usar Azure Key Vault.

## Azure

La incorporación es progresiva. El orden previsto está documentado en `docs/azure.md`.

Preferir servicios administrados de Azure antes que máquinas virtuales.

## Respuestas

Cuando propongas una implementación:
1. Explica brevemente por qué.
2. Menciona ventajas y desventajas solo si son relevantes.
3. Luego muestra el código.
4. Si la propuesta contradice las reglas del proyecto, adviértelo.
5. No asumir requisitos que no fueron definidos.

# Reglas criticas

- Nunca hacer commit.
- Nunca hacer push.
- Nunca crear PRs.
- Nunca modificar archivos no relacionados.
- Preguntar antes de hacer cambios de arquitectura.
