# ServiceDesk — Registro de tareas

## Última tarea completada

**Fase D — Verificación y documentación del value object `WeeklySchedule`**: 38 tests nuevos —
`WeeklyScheduleTests` (invariantes Create, default, días cerrados, `HasEnabledWindows`),
`WeeklyScheduleJsonSerializerTests` (canónico, legacy PascalCase, claves en español, días faltantes,
claves desconocidas, 10 casos inválidos, orden Monday-first, round-trip) y ampliación de
`BusinessHoursCalculatorTests` (`CalculateElapsed`, `AddBusinessHours` con carry-over/guard/límites,
`CalculatePercentageElapsed`, bordes de ventana). Ajuste en serializador: opciones de escritura
propias que omiten nulos (días cerrados sin start/end) y salida canónica ordenada Monday→Sunday.
Verificado: suite completa 200/200 unit + 78/78 integración; decisión documentada en
docs/architecture.md ("Decisiones registradas").

## Tarea actual

Ninguna asignada.

## Decisiones importantes

- Serialización/deserialización JSON únicamente en Infrastructure; Domain no conoce System.Text.Json
  (abstracción `IWeeklyScheduleSerializer` en Application, implementada e inyectada desde Infrastructure).
- Contrato HTTP intacto (`businessHoursJson: string` en DTOs): frontend sin cambios.
- `AddBusinessHours` devuelve `fromUtc` si el schedule no tiene ventanas habilitadas (evita bucle infinito).
- Materialización desde BD con datos corruptos hace fallback a horario default; escritura siempre validada.
- Columna física `BusinessHoursJson` conservada vía `HasColumnName`; migración no-op.

## Lo siguiente

Sin tarea asignada.