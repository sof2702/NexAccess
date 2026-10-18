# Arquitectura

NexAccess es una Web API ASP.NET Core organizada en cuatro proyectos de producción y dos de pruebas. Las dependencias van en un solo sentido:

```
NexAccess.API ──► NexAccess.Application ──► NexAccess.Domain
      │                    ▲
      └──► NexAccess.Infrastructure ──┘
```

`Infrastructure` implementa los contratos definidos en `Application` (repositorios, unidad de trabajo, hash de contraseñas, tokens, claves de dispositivo y reloj). `Application` no conoce EF Core, ASP.NET ni PostgreSQL.

## Estructura

```
NexAccess/
├── NexAccess.sln
├── README.md
├── docs/
├── src/
│   ├── NexAccess.API/
│   │   ├── Controllers/        Controladores HTTP (los únicos del repositorio)
│   │   ├── Middleware/         Manejo centralizado de errores y respuestas ProblemDetails
│   │   ├── Configuration/      JWT, esquema X-DEVICE-KEY, OpenAPI, usuario actual, validación en español
│   │   ├── Properties/launchSettings.json
│   │   ├── Program.cs
│   │   └── appsettings*.json   Sin secretos
│   ├── NexAccess.Application/
│   │   ├── DTOs/               Contratos de entrada y salida por módulo
│   │   ├── Exceptions/         Errores de negocio con código estable
│   │   ├── Interfaces/         IClock, ICurrentUser, IUnitOfWork, seguridad y Repositories/
│   │   ├── Options/            Access, Attendance y Enrollment
│   │   ├── Services/           Casos de uso (un servicio por módulo) y Reports/CsvBuilder
│   │   └── Validators/         DataAnnotations, código de credencial y rangos de fechas
│   ├── NexAccess.Domain/
│   │   ├── Entities/           Entidades y reglas propias (permiso por horario, revocación, vencimiento)
│   │   └── Enums/              Enumeraciones y motivos estables de rechazo
│   └── NexAccess.Infrastructure/
│       ├── Persistence/
│       │   ├── NexAccessDbContext.cs
│       │   ├── Configurations/ Mapeo de cada entidad a tablas y columnas en español
│       │   ├── Repositories/   Implementaciones EF Core
│       │   ├── Scripts/        SQL idempotente compatible con PostgreSQL 9.3
│       │   └── UnitOfWork.cs   Traducción de errores de PostgreSQL
│       ├── Security/           PBKDF2, JWT y claves de dispositivo
│       └── Services/           Reloj del sistema
└── tests/
    ├── NexAccess.UnitTests/         Servicios con dobles de prueba (NSubstitute) y componentes de infraestructura
    └── NexAccess.IntegrationTests/  API real con WebApplicationFactory contra PostgreSQL de prueba
```

## Responsabilidades

| Capa | Responsabilidad |
|---|---|
| API | Rutas `/api/v1`, autenticación (JWT y `X-DEVICE-KEY`), autorización por roles, serialización JSON, OpenAPI, límite de intentos de login y traducción de excepciones a HTTP. Los controladores solo delegan en los servicios. |
| Application | Reglas de negocio y validaciones: decisión de acceso, alternancia de entradas y salidas, enrolamiento con vencimiento, unicidad, auditoría y reportes. |
| Domain | Entidades sin dependencias externas. Reglas intrínsecas: `AccessPermission.AllowsAt`, `Credential.Revoke`, `Credential.IsExpiredAt`, `Device.ControlsAccess`. |
| Infrastructure | Acceso a PostgreSQL con EF Core (sin migraciones automáticas), consultas, traducción de errores (`23505`, `23503` y `23514` → 409) y servicios criptográficos. |

## Flujo de una verificación de acceso

1. El ESP32 envía `POST /api/v1/access/verify` con `X-DEVICE-KEY`.
2. `DeviceKeyAuthenticationHandler` calcula el SHA-256 de la clave, busca el dispositivo activo y actualiza `ultima_conexion` (como máximo cada 30 s).
3. `AccessController` delega en `AccessService.VerifyAsync`.
4. `AccessService` valida, en este orden: código del dispositivo, formato de la credencial, existencia, revocación, estado, vencimiento, empleado activo, área del dispositivo, área activa y, si el área es restringida, los permisos de los roles activos del empleado para el día y la hora local.
5. Siempre registra un `evento_acceso`, autorizado o rechazado. Si el dispositivo es `MARCACION` o `MIXTO` y el acceso fue autorizado, `AttendanceService` registra la marcación (o la marca como duplicada).
6. Responde con un motivo estable (`AUTHORIZED`, `CREDENTIAL_INACTIVE`, etc.).

## Convenciones

- El código no tiene comentarios; la explicación vive en `docs/`.
- Los nombres de C# están en inglés y las tablas y columnas en español, como en el esquema original. El mapeo está en `Persistence/Configurations`.
- Las enumeraciones se guardan como texto en MAYÚSCULAS (`UpperSnakeEnumConverter`) y se serializan igual en JSON (`FISICA`, `ENROLL_CARD`, `RRHH`).
- Las fechas se guardan en UTC (`TIMESTAMPTZ`). Los horarios de permisos y los reportes diarios usan la zona `Access:TimeZoneId` (por defecto `America/Asuncion`).
- Los errores se devuelven como `application/problem+json` con `code` y `traceId`.
