# NexAccess

Web API para el control de acceso físico y el registro de asistencia con credenciales NFC físicas y digitales, administración de permisos por roles y terminales ESP32.

- **Tecnologías:** .NET 10, ASP.NET Core Web API, EF Core 10 + Npgsql 10, PostgreSQL, JWT Bearer, xUnit + NSubstitute.
- **Usuarios administrativos:** JWT con los roles `ADMIN`, `SEGURIDAD` y `RRHH`.
- **Dispositivos ESP32:** encabezado `X-DEVICE-KEY` con una clave individual por dispositivo.
- **Documentación de la API:** OpenAPI en `/openapi/v1.json` y Swagger UI en `/swagger`. Se habilitan en Development o con `OpenApi:Enabled=true`.

## Estado

| Área | Estado |
|---|---|
| Autenticación JWT, usuarios y roles del sistema | Implementado y probado |
| Empleados, áreas, roles y permisos de acceso por horario | Implementado y probado |
| Credenciales NFC (registro, activación, desactivación, revocación y vencimiento) | Implementado y probado |
| Dispositivos (alta, clave individual, rotación de clave, activación y última conexión) | Implementado y probado |
| Comandos y enrolamiento de tarjetas desde el ESP32 | Implementado y probado (API) |
| Verificación de acceso `POST /api/v1/access/verify` | Implementado y probado (API) |
| Asistencia (entrada/salida automática, anti-duplicados, correcciones manuales, reporte diario y CSV) | Implementado y probado |
| Eventos de acceso, auditoría y exportación CSV | Implementado y probado |
| Firmware ESP32 y prueba con hardware real | **Pendiente** (fuera de este repositorio) |
| Portal web y aplicación móvil | **Pendiente** (fuera de este repositorio) |
| Conexión contra PostgreSQL 9.3.5 | **No verificada**: se probó contra PostgreSQL 18.3 local |

## Inicio rápido (desarrollo)

```powershell
git clone https://github.com/sof2702/NexAccess
cd NexAccess

# 1. Base de datos (crear y aplicar scripts; no son destructivos)
psql -h localhost -U postgres -c "CREATE DATABASE nexaccess_dev ENCODING 'UTF8' TEMPLATE template0"
psql -h localhost -U postgres -d nexaccess_dev -f src/NexAccess.Infrastructure/Persistence/Scripts/001_esquema_inicial.sql
psql -h localhost -U postgres -d nexaccess_dev -f src/NexAccess.Infrastructure/Persistence/Scripts/002_datos_iniciales.sql

# 2. Secretos locales (nunca en appsettings ni en Git)
dotnet user-secrets set "ConnectionStrings:NexAccess" "Host=localhost;Database=nexaccess_dev;Username=postgres;Password=<clave>" --project src/NexAccess.API
dotnet user-secrets set "Jwt:Key" "<clave aleatoria de al menos 32 bytes>" --project src/NexAccess.API
dotnet user-secrets set "Bootstrap:AdminUsername" "admin" --project src/NexAccess.API
dotnet user-secrets set "Bootstrap:AdminPassword" "<contraseña de al menos 12 caracteres>" --project src/NexAccess.API

# 3. Ejecutar
dotnet run --project src/NexAccess.API --launch-profile http
# http://localhost:5131/swagger
```

## Pruebas

```powershell
dotnet test tests/NexAccess.UnitTests
dotnet user-secrets set "ConnectionStrings:NexAccessTest" "Host=localhost;Database=nexaccess_test;Username=postgres;Password=<clave>" --id nexaccess-integration-tests
dotnet test tests/NexAccess.IntegrationTests
```

Las pruebas de integración crean la base `nexaccess_test` si no existe, aplican los scripts y generan sus propios datos con códigos únicos. Se niegan a ejecutarse contra una base cuyo nombre no contenga `test`.

## Documentación

- [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md): estructura, capas y responsabilidades.
- [docs/BASE_DE_DATOS.md](docs/BASE_DE_DATOS.md): esquema, relaciones, scripts y compatibilidad con PostgreSQL 9.3.
- [docs/API_ESP32.md](docs/API_ESP32.md): contratos HTTP para los dispositivos.
- [docs/SEGURIDAD.md](docs/SEGURIDAD.md): autenticación, autorización, secretos y advertencias.
- [docs/INSTALACION.md](docs/INSTALACION.md): configuración local y despliegue.
- [docs/MANUAL_USUARIO.md](docs/MANUAL_USUARIO.md): manual para operadores de RRHH, Seguridad y administradores.
