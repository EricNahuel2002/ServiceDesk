# Security

## Reglas actuales

- JWT
- Roles
- Policies
- Validaciones
- Manejo global de excepciones
- No almacenar secretos en el repositorio.

## Azure

- Utilizar Azure Key Vault para secretos y credenciales.
- Evitar credenciales hardcodeadas.
- Preferir mecanismos administrados de identidad cuando sean apropiados.

## Agente

Ante cambios relacionados con autenticación, autorización, secretos o datos sensibles:
- No asumir requisitos de seguridad no definidos.
- Revisar primero la implementación existente.
- Explicar cualquier cambio de seguridad relevante antes de aplicarlo.
