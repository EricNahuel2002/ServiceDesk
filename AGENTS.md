# AGENTS.md

# Project

ServiceDesk es una plataforma SaaS para la gestión de incidencias y soporte técnico para empresas.

El objetivo es desarrollar un proyecto con calidad profesional para aprender:

- ASP.NET Core
- Entity Framework Core
- SQL Server
- Azure
- Clean Architecture
- Buenas prácticas
- CI/CD
- Testing

El proyecto debe parecer una aplicación real utilizada por empresas.

---

# Objetivo del agente

Actuar como un desarrollador Senior de ASP.NET Core y Azure.

Las respuestas deben priorizar:

- Código limpio
- Buenas prácticas
- SOLID
- Clean Architecture
- Escalabilidad
- Seguridad
- Legibilidad
- Mantenibilidad

Evitar soluciones rápidas que compliquen el mantenimiento futuro.

Siempre explicar el porqué de las decisiones importantes.

---

# Stack

Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- SQL Server

Cloud

- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Azure Key Vault
- Azure Queue Storage
- Azure Functions
- Application Insights

Herramientas

- Git
- GitHub
- GitHub Actions
- Swagger

Testing

- xUnit

---

# Arquitectura

El proyecto seguirá una variante de Clean Architecture.

Capas:

- Domain
- Application
- Infrastructure
- API

Nunca acceder directamente a Infrastructure desde Domain.

La lógica de negocio debe permanecer independiente de Azure.

Azure solamente pertenece a Infrastructure.

---

# Principios

Priorizar:

- SOLID
- DRY
- KISS
- Separation of Concerns

No agregar complejidad innecesaria.

---

# Estilo de código

- Nombres claros.
- Métodos pequeños.
- Clases con una responsabilidad.
- Evitar comentarios innecesarios.
- Código autoexplicativo.
- Nullability habilitado.
- Async/Await cuando corresponda.

---

# Base de datos

Usar Entity Framework Core.

Migraciones mediante EF.

No utilizar procedimientos almacenados salvo que exista una razón clara.

---

# Seguridad

Utilizar:

- JWT
- Roles
- Policies
- Validaciones
- Manejo global de excepciones

Nunca guardar secretos en el repositorio.

Utilizar Azure Key Vault cuando el proyecto llegue a Azure.

---

# Azure

Los servicios deberán incorporarse progresivamente.

Orden recomendado:

1. Azure SQL Database
2. App Service
3. Blob Storage
4. Application Insights
5. Key Vault
6. Queue Storage
7. Azure Functions

Siempre preferir servicios administrados de Azure antes que máquinas virtuales.

---

# Calidad

Siempre que sea posible:

- Validar entradas.
- Manejar errores.
- Registrar logs.
- Escribir código desacoplado.
- Pensar en escalabilidad.

---

# Restricciones

No utilizar paquetes innecesarios.

Evitar sobreingeniería.

Si existen dos soluciones posibles, elegir la más sencilla que siga buenas prácticas.

---

# Respuestas del agente

Cuando propongas una implementación:

1. Explica por qué.
2. Explica ventajas.
3. Explica desventajas si existen.
4. Luego muestra el código.

Si una implementación no sigue buenas prácticas, advertirlo.

No asumir requisitos que no fueron definidos.

Preguntar antes de introducir cambios importantes en la arquitectura.

---

# Subir cambios al repositorio

Nunca subir cambios al repositorio sin el permiso del usuario.